namespace Teryaq.IntegrationTests.Features.Customers;

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

/// <summary>Integration tests for the <c>/api/v1/customers</c> endpoint.</summary>
[Collection(IntegrationTestSuite.Name)]
public sealed class CustomersControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Initialises a new instance of <see cref="CustomersControllerTests"/>.</summary>
    public CustomersControllerTests(CustomWebApplicationFactory factory)
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
    // POST /api/v1/customers
    // -------------------------------------------------------------------------

    /// <summary>An unauthenticated POST must return 401.</summary>
    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/customers", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A valid customer request returns 201 with the created customer.</summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        string token = await RegisterAndGetTokenAsync("Customer Pharmacy", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { Name = "Ahmed Ali", Phone = "01012345678", Email = (string?)null };
        var response = await _client.PostAsJsonAsync("/api/v1/customers", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        string body = await response.Content.ReadAsStringAsync();
        var customer = JsonSerializer.Deserialize<CustomerResponse>(body, JsonOptions);
        customer.ShouldNotBeNull();
        customer.Name.ShouldBe("Ahmed Ali");
        customer.Phone.ShouldBe("01012345678");
    }

    /// <summary>An empty name returns 422.</summary>
    [Fact]
    public async Task Create_EmptyName_Returns422()
    {
        string token = await RegisterAndGetTokenAsync("Validation Pharmacy", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { Name = string.Empty, Phone = (string?)null, Email = (string?)null };
        var response = await _client.PostAsJsonAsync("/api/v1/customers", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/customers/{id}
    // -------------------------------------------------------------------------

    /// <summary>A created customer can be retrieved by its identifier.</summary>
    [Fact]
    public async Task GetById_ExistingCustomer_Returns200()
    {
        string token = await RegisterAndGetTokenAsync("Get Customer Pharm", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await CreateCustomerAndGetIdAsync("Sara Hassan", "01098765432");

        var response = await _client.GetAsync($"/api/v1/customers/{customerId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var customer = JsonSerializer.Deserialize<CustomerResponse>(body, JsonOptions);
        customer.ShouldNotBeNull();
        customer.Id.ShouldBe(customerId);
        customer.Name.ShouldBe("Sara Hassan");
    }

    /// <summary>Requesting a non-existent customer returns 404.</summary>
    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        string token = await RegisterAndGetTokenAsync("Not Found Pharm", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/customers/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/customers (paged)
    // -------------------------------------------------------------------------

    /// <summary>GET returns 200 with the created customer in the list.</summary>
    [Fact]
    public async Task GetPaged_ReturnsCreatedCustomer()
    {
        string token = await RegisterAndGetTokenAsync("List Pharmacy", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customerId = await CreateCustomerAndGetIdAsync("Mohamed Youssef", null);

        var response = await _client.GetAsync("/api/v1/customers?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<PageResponse<CustomerResponse>>(body, JsonOptions);
        page.ShouldNotBeNull();
        page.Items.ShouldContain(c => c.Id == customerId);
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    /// <summary>Tenant A's customer is not visible to Tenant B.</summary>
    [Fact]
    public async Task TenantIsolation_TenantBCannotSeeTenantACustomer()
    {
        // Tenant A
        string tokenA = await RegisterAndGetTokenAsync("Tenant A Cust", "Branch A");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var customerIdA = await CreateCustomerAndGetIdAsync("Customer A", "011111111");

        // Tenant B
        string tokenB = await RegisterAndGetTokenAsync("Tenant B Cust", "Branch B");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync($"/api/v1/customers/{customerIdA}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<string> RegisterAndGetTokenAsync(string pharmacyName, string branchName)
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
        var auth = JsonSerializer.Deserialize<AuthResponse>(body, JsonOptions);
        auth.ShouldNotBeNull();
        return auth.AccessToken;
    }

    private async Task<Guid> CreateCustomerAndGetIdAsync(string name, string? phone)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/customers", new
        {
            Name = name,
            Phone = phone,
            Email = (string?)null,
        });
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var customer = JsonSerializer.Deserialize<CustomerResponse>(body, JsonOptions);
        customer.ShouldNotBeNull();
        return customer.Id;
    }

    // -------------------------------------------------------------------------
    // Local response models
    // -------------------------------------------------------------------------

    private sealed class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class CustomerResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    private sealed class PageResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }
}
