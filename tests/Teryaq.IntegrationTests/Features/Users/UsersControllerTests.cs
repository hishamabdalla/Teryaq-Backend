namespace Teryaq.IntegrationTests.Features.Users;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Teryaq.Infrastructure.Persistence;
using Xunit;

/// <summary>Integration tests for the <c>/api/v1/users</c> endpoint covering invite, update, deactivate, and tenant isolation.</summary>
[Collection(IntegrationTestSuite.Name)]
public sealed class UsersControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Initialises a new instance of <see cref="UsersControllerTests"/>.</summary>
    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/users
    // -------------------------------------------------------------------------

    /// <summary>An unauthenticated request must return 401.</summary>
    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>An Owner sees themselves in the user list after registering.</summary>
    [Fact]
    public async Task GetAll_AuthenticatedOwner_ReturnsOkWithOwnerInList()
    {
        string ownerEmail = $"owner_{Guid.NewGuid():N}@test.com";
        string token = await RegisterOwnerAndGetTokenAsync("Users Pharmacy", ownerEmail);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserResponse>>(body, JsonOptions);

        users.ShouldNotBeNull();
        users.Count.ShouldBe(1);
        users[0].Email.ShouldBe(ownerEmail);
        users[0].Role.ShouldBe("Owner");
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/users (invite)
    // -------------------------------------------------------------------------

    /// <summary>Inviting a new pharmacist with valid data returns 201 Created.</summary>
    [Fact]
    public async Task Invite_ValidRequest_Returns201()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Invite Pharmacy");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string pharmacistEmail = $"pharmacist_{Guid.NewGuid():N}@test.com";

        var response = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Email = pharmacistEmail,
            FullName = "Ahmed Hassan",
            Password = "Staff@12345",
            BranchId = (Guid?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        string body = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserResponse>(body, JsonOptions);

        user.ShouldNotBeNull();
        user.Email.ShouldBe(pharmacistEmail);
        user.FullName.ShouldBe("Ahmed Hassan");
        user.Role.ShouldBe("Pharmacist");
        user.IsLocked.ShouldBeFalse();
    }

    /// <summary>Inviting a user with an email that already exists returns 409 Conflict.</summary>
    [Fact]
    public async Task Invite_DuplicateEmail_Returns409()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Duplicate Pharmacy");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string pharmacistEmail = $"pharmacist_{Guid.NewGuid():N}@test.com";
        var payload = new { Email = pharmacistEmail, FullName = "Test User", Password = "Staff@12345", BranchId = (Guid?)null };

        await _client.PostAsJsonAsync("/api/v1/users", payload);
        var duplicate = await _client.PostAsJsonAsync("/api/v1/users", payload);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>Inviting a user with invalid email returns 422 Unprocessable Entity.</summary>
    [Fact]
    public async Task Invite_InvalidEmail_Returns422()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Validation Pharmacy");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Email = "not-an-email",
            FullName = "Test",
            Password = "Staff@12345",
            BranchId = (Guid?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/users/{id}
    // -------------------------------------------------------------------------

    /// <summary>Updating a user's name returns 200 OK with the updated data.</summary>
    [Fact]
    public async Task Update_ValidRequest_Returns200WithUpdatedName()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Update Users Pharmacy");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invited = await InvitePharmacistAsync("Updated Pharmacist");

        var response = await _client.PutAsJsonAsync($"/api/v1/users/{invited.Id}", new
        {
            FullName = "Updated Full Name",
            BranchId = (Guid?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserResponse>(body, JsonOptions);

        user.ShouldNotBeNull();
        user.FullName.ShouldBe("Updated Full Name");
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/users/{id}/deactivate
    // -------------------------------------------------------------------------

    /// <summary>Deactivating a pharmacist returns 200 OK with IsLocked true.</summary>
    [Fact]
    public async Task Deactivate_ExistingUser_Returns200AndIsLockedTrue()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Deactivate Pharmacy");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invited = await InvitePharmacistAsync("To Be Deactivated");

        var response = await _client.PutAsJsonAsync($"/api/v1/users/{invited.Id}/deactivate", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserResponse>(body, JsonOptions);

        user.ShouldNotBeNull();
        user.IsLocked.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    /// <summary>A user from Tenant A must not appear in Tenant B's user list.</summary>
    [Fact]
    public async Task TenantIsolation_UserFromOtherTenant_IsNotVisible()
    {
        // Register Tenant A and invite a pharmacist
        string tokenA = await RegisterOwnerAndGetTokenAsync("Isolation Pharmacy A");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await InvitePharmacistAsync("Tenant A Pharmacist");

        // Register Tenant B
        string tokenB = await RegisterOwnerAndGetTokenAsync("Isolation Pharmacy B");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserResponse>>(body, JsonOptions);

        users.ShouldNotBeNull();
        users.ShouldAllBe(u => !u.FullName.StartsWith("Tenant A", StringComparison.Ordinal));
        users.Count.ShouldBe(1); // only the Owner of Tenant B
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<string> RegisterOwnerAndGetTokenAsync(string pharmacyName, string? ownerEmail = null)
    {
        ownerEmail ??= $"owner_{Guid.NewGuid():N}@test.com";
        var payload = new
        {
            PharmacyName = pharmacyName,
            BranchName = "Main Branch",
            BranchAddress = (string?)null,
            BranchPhone = (string?)null,
            OwnerEmail = ownerEmail,
            OwnerPassword = "Test@12345",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<AuthTokenResponse>(body, JsonOptions);
        auth.ShouldNotBeNull();
        return auth.AccessToken;
    }

    private async Task<UserResponse> InvitePharmacistAsync(string fullName)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Email = $"pharmacist_{Guid.NewGuid():N}@test.com",
            FullName = fullName,
            Password = "Staff@12345",
            BranchId = (Guid?)null,
        });
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserResponse>(body, JsonOptions);
        user.ShouldNotBeNull();
        return user;
    }

    // -------------------------------------------------------------------------
    // Local response models
    // -------------------------------------------------------------------------

    private sealed class AuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
        public bool IsLocked { get; set; }
    }
}
