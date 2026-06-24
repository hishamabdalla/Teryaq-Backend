namespace Teryaq.IntegrationTests.Features.Branches;

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

/// <summary>Integration tests for the <c>/api/v1/branches</c> endpoint covering CRUD and tenant isolation.</summary>
[Collection(IntegrationTestSuite.Name)]
public sealed class BranchesControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Initialises a new instance of <see cref="BranchesControllerTests"/>.</summary>
    public BranchesControllerTests(CustomWebApplicationFactory factory)
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
    // GET /api/v1/branches
    // -------------------------------------------------------------------------

    /// <summary>An unauthenticated request must return 401.</summary>
    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/branches");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>An Owner sees only their tenant's branches after registering.</summary>
    [Fact]
    public async Task GetAll_AuthenticatedOwner_ReturnsOkWithMainBranch()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Branch Test Pharmacy", "Main Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/branches");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var branches = JsonSerializer.Deserialize<List<BranchResponse>>(body, JsonOptions);

        branches.ShouldNotBeNull();
        branches.Count.ShouldBe(1);
        branches[0].Name.ShouldBe("Main Branch");
        branches[0].IsMain.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/branches
    // -------------------------------------------------------------------------

    /// <summary>Creating a branch with valid data returns 201 Created.</summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Pharmacy A", "Main A");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/branches", new
        {
            Name = "Branch Two",
            Address = "10 Cairo Street",
            Phone = "010-0000001",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        string body = await response.Content.ReadAsStringAsync();
        var branch = JsonSerializer.Deserialize<BranchResponse>(body, JsonOptions);

        branch.ShouldNotBeNull();
        branch.Name.ShouldBe("Branch Two");
        branch.IsMain.ShouldBeFalse();
        branch.IsActive.ShouldBeTrue();
    }

    /// <summary>Creating a branch with a duplicate name returns 409 Conflict.</summary>
    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Pharmacy Dup", "Main Dup");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { Name = "Same Name", Address = (string?)null, Phone = (string?)null };
        await _client.PostAsJsonAsync("/api/v1/branches", payload);
        var duplicate = await _client.PostAsJsonAsync("/api/v1/branches", payload);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>Creating a branch with a missing name returns 422 Unprocessable Entity.</summary>
    [Fact]
    public async Task Create_MissingName_Returns422()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Pharmacy Val", "Main Val");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/branches", new
        {
            Name = string.Empty,
            Address = (string?)null,
            Phone = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/branches/{id}
    // -------------------------------------------------------------------------

    /// <summary>Updating a branch with valid data returns 200 OK with updated values.</summary>
    [Fact]
    public async Task Update_ValidRequest_Returns200()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Pharmacy Update", "Update Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await CreateBranchAsync("Original Name");

        var response = await _client.PutAsJsonAsync($"/api/v1/branches/{created.Id}", new
        {
            Name = "Updated Name",
            Address = "New Address",
            Phone = "011-9999999",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var branch = JsonSerializer.Deserialize<BranchResponse>(body, JsonOptions);

        branch.ShouldNotBeNull();
        branch.Name.ShouldBe("Updated Name");
        branch.Address.ShouldBe("New Address");
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/branches/{id}
    // -------------------------------------------------------------------------

    /// <summary>Deleting a non-primary branch returns 204 No Content.</summary>
    [Fact]
    public async Task Delete_NonPrimaryBranch_Returns204()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Pharmacy Del", "Del Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await CreateBranchAsync("Branch To Delete");

        var response = await _client.DeleteAsync($"/api/v1/branches/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>Deleting the primary branch returns 409 Conflict.</summary>
    [Fact]
    public async Task Delete_PrimaryBranch_Returns409()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Pharmacy NoDel", "Primary Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get the main branch (registered as "Primary Branch")
        var listResponse = await _client.GetAsync("/api/v1/branches");
        string listBody = await listResponse.Content.ReadAsStringAsync();
        var branches = JsonSerializer.Deserialize<List<BranchResponse>>(listBody, JsonOptions);
        branches.ShouldNotBeNull();
        var main = branches.First(b => b.IsMain);

        var response = await _client.DeleteAsync($"/api/v1/branches/{main.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/branches/{id}/deactivate
    // -------------------------------------------------------------------------

    /// <summary>Deactivating a non-primary branch returns 200 with IsActive false.</summary>
    [Fact]
    public async Task Deactivate_NonPrimaryBranch_Returns200AndIsActiveFalse()
    {
        string token = await RegisterOwnerAndGetTokenAsync("Pharmacy Deact", "Deact Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await CreateBranchAsync("Branch To Deactivate");

        var response = await _client.PutAsJsonAsync($"/api/v1/branches/{created.Id}/deactivate", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var branch = JsonSerializer.Deserialize<BranchResponse>(body, JsonOptions);

        branch.ShouldNotBeNull();
        branch.IsActive.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    /// <summary>A branch created by Tenant A must not appear in Tenant B's branch list.</summary>
    [Fact]
    public async Task TenantIsolation_BranchFromOtherTenant_IsNotVisible()
    {
        // Register Tenant A and create an extra branch
        string tokenA = await RegisterOwnerAndGetTokenAsync("Tenant A Pharmacy", "Tenant A Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await CreateBranchAsync("Tenant A Branch");

        // Register Tenant B
        string tokenB = await RegisterOwnerAndGetTokenAsync("Tenant B Pharmacy", "Tenant B Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync("/api/v1/branches");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var branches = JsonSerializer.Deserialize<List<BranchResponse>>(body, JsonOptions);

        branches.ShouldNotBeNull();
        branches.ShouldAllBe(b => !b.Name.StartsWith("Tenant A", StringComparison.Ordinal));
        branches.Count.ShouldBe(1); // only the main branch of Tenant B
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<string> RegisterOwnerAndGetTokenAsync(string pharmacyName, string branchName)
    {
        var payload = new
        {
            PharmacyName = pharmacyName,
            BranchName = branchName,
            BranchAddress = (string?)null,
            BranchPhone = (string?)null,
            OwnerEmail = $"owner_{Guid.NewGuid():N}@test.com",
            OwnerPassword = "Test@12345",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<AuthTokenResponse>(body, JsonOptions);
        auth.ShouldNotBeNull();
        return auth.AccessToken;
    }

    private async Task<BranchResponse> CreateBranchAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/branches", new
        {
            Name = name,
            Address = (string?)null,
            Phone = (string?)null,
        });
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var branch = JsonSerializer.Deserialize<BranchResponse>(body, JsonOptions);
        branch.ShouldNotBeNull();
        return branch;
    }

    // -------------------------------------------------------------------------
    // Local response models
    // -------------------------------------------------------------------------

    private sealed class AuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class BranchResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsMain { get; set; }
        public bool IsActive { get; set; }
    }
}
