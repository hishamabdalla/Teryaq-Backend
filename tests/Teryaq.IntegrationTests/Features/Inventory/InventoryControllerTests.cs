namespace Teryaq.IntegrationTests.Features.Inventory;

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

/// <summary>Integration tests for the <c>/api/v1/inventory</c> endpoint covering receive, adjust, delete and tenant isolation.</summary>
[Collection(IntegrationTestSuite.Name)]
public sealed class InventoryControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Initialises a new instance of <see cref="InventoryControllerTests"/>.</summary>
    public InventoryControllerTests(CustomWebApplicationFactory factory)
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
    // GET /api/v1/inventory
    // -------------------------------------------------------------------------

    /// <summary>An unauthenticated request must return 401.</summary>
    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/inventory");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Authenticated staff sees empty list before any batches are received.</summary>
    [Fact]
    public async Task GetAll_Authenticated_ReturnsEmptyList()
    {
        string token = await RegisterAndGetTokenAsync("Inventory Pharmacy", "Main Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/inventory");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<PageResponse<BatchResponse>>(body, JsonOptions);
        page.ShouldNotBeNull();
        page.TotalCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/inventory
    // -------------------------------------------------------------------------

    /// <summary>Receiving a valid batch returns 201 Created with a Location header.</summary>
    [Fact]
    public async Task Receive_ValidRequest_Returns201WithLocation()
    {
        string token = await RegisterAndGetTokenAsync("Receive Pharmacy", "Receive Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/inventory", new
        {
            BranchId = branchId,
            DrugId = drugId,
            BatchNumber = "LOT-001",
            ExpiryDate = "2027-12-31",
            Quantity = 100,
            CostPrice = 10.00,
            SellingPrice = (decimal?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        string body = await response.Content.ReadAsStringAsync();
        var batch = JsonSerializer.Deserialize<BatchResponse>(body, JsonOptions);
        batch.ShouldNotBeNull();
        batch.BatchNumber.ShouldBe("LOT-001");
        batch.QuantityReceived.ShouldBe(100);
        batch.QuantityOnHand.ShouldBe(100);
    }

    /// <summary>Receiving with a non-existent drug returns 404.</summary>
    [Fact]
    public async Task Receive_UnknownDrug_Returns404()
    {
        string token = await RegisterAndGetTokenAsync("Drug404 Pharmacy", "Drug404 Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/inventory", new
        {
            BranchId = branchId,
            DrugId = Guid.NewGuid(),
            BatchNumber = "LOT-X",
            ExpiryDate = "2027-12-31",
            Quantity = 10,
            CostPrice = 5.00,
            SellingPrice = (decimal?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Receiving with a non-existent branch returns 404.</summary>
    [Fact]
    public async Task Receive_UnknownBranch_Returns404()
    {
        string token = await RegisterAndGetTokenAsync("Branch404 Pharmacy", "Branch404 Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var drugId = await CreateDrugAndGetIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/inventory", new
        {
            BranchId = Guid.NewGuid(),
            DrugId = drugId,
            BatchNumber = "LOT-B",
            ExpiryDate = "2027-12-31",
            Quantity = 10,
            CostPrice = 5.00,
            SellingPrice = (decimal?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Receiving with a past expiry date returns 422.</summary>
    [Fact]
    public async Task Receive_PastExpiryDate_Returns422()
    {
        string token = await RegisterAndGetTokenAsync("Expiry422 Pharmacy", "Expiry422 Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/inventory", new
        {
            BranchId = branchId,
            DrugId = drugId,
            BatchNumber = "LOT-EXPIRED",
            ExpiryDate = "2020-01-01",
            Quantity = 10,
            CostPrice = 5.00,
            SellingPrice = (decimal?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/inventory/{id}
    // -------------------------------------------------------------------------

    /// <summary>Get by id returns 200 with the created batch details.</summary>
    [Fact]
    public async Task GetById_AfterReceive_Returns200WithDetails()
    {
        string token = await RegisterAndGetTokenAsync("GetById Pharmacy", "GetById Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        var batch = await ReceiveBatchAsync(branchId, drugId, "LOT-GETID", 50);

        var response = await _client.GetAsync($"/api/v1/inventory/{batch.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var found = JsonSerializer.Deserialize<BatchResponse>(body, JsonOptions);
        found.ShouldNotBeNull();
        found.Id.ShouldBe(batch.Id);
        found.BatchNumber.ShouldBe("LOT-GETID");
        found.DrugTradeNameEn.ShouldBe("Paracetamol");
        found.BranchName.ShouldBe("GetById Main");
    }

    /// <summary>Get by unknown id returns 404.</summary>
    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        string token = await RegisterAndGetTokenAsync("GetById404 Pharmacy", "GetById404 Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/inventory/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/inventory/{id}
    // -------------------------------------------------------------------------

    /// <summary>Valid adjustment returns 200 with updated values.</summary>
    [Fact]
    public async Task Adjust_ValidRequest_Returns200WithUpdatedValues()
    {
        string token = await RegisterAndGetTokenAsync("Adjust Pharmacy", "Adjust Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        var batch = await ReceiveBatchAsync(branchId, drugId, "LOT-ADJ", 100);

        var response = await _client.PutAsJsonAsync($"/api/v1/inventory/{batch.Id}", new
        {
            QuantityOnHand = 85,
            SellingPrice = 18.50,
            ExpiryDate = "2027-06-30",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<BatchResponse>(body, JsonOptions);
        updated.ShouldNotBeNull();
        updated.QuantityOnHand.ShouldBe(85);
        updated.SellingPrice.ShouldBe(18.50m);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/inventory/{id}
    // -------------------------------------------------------------------------

    /// <summary>Deleting an existing batch returns 204.</summary>
    [Fact]
    public async Task Delete_ExistingBatch_Returns204()
    {
        string token = await RegisterAndGetTokenAsync("Delete Pharmacy", "Delete Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        var batch = await ReceiveBatchAsync(branchId, drugId, "LOT-DEL", 20);

        var response = await _client.DeleteAsync($"/api/v1/inventory/{batch.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify it's gone from the list
        var listResponse = await _client.GetAsync("/api/v1/inventory");
        string listBody = await listResponse.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<PageResponse<BatchResponse>>(listBody, JsonOptions);
        page.ShouldNotBeNull();
        page.Items.ShouldNotContain(b => b.Id == batch.Id);
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    /// <summary>Tenant A's batches are invisible to Tenant B.</summary>
    [Fact]
    public async Task TenantIsolation_BatchFromOtherTenant_IsNotVisible()
    {
        // Tenant A receives stock
        string tokenA = await RegisterAndGetTokenAsync("Tenant A Inv", "Tenant A Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var branchAId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        var batchA = await ReceiveBatchAsync(branchAId, drugId, "LOT-TENANT-A", 10);

        // Tenant B lists their inventory
        string tokenB = await RegisterAndGetTokenAsync("Tenant B Inv", "Tenant B Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync("/api/v1/inventory");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<PageResponse<BatchResponse>>(body, JsonOptions);
        page.ShouldNotBeNull();
        page.Items.ShouldNotContain(b => b.Id == batchA.Id);
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
        var auth = JsonSerializer.Deserialize<AuthTokenResponse>(body, JsonOptions);
        auth.ShouldNotBeNull();
        return auth.AccessToken;
    }

    private async Task<Guid> GetMainBranchIdAsync()
    {
        var response = await _client.GetAsync("/api/v1/branches");
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var branches = JsonSerializer.Deserialize<List<BranchResponse>>(body, JsonOptions);
        branches.ShouldNotBeNull();
        return branches.First(b => b.IsMain).Id;
    }

    private async Task<Guid> CreateDrugAndGetIdAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/drugs", new
        {
            TradeNameAr = "باراسيتامول",
            TradeNameEn = "Paracetamol",
            GenericName = "Paracetamol",
            DosageForm = "Tablet",
            Strength = "500mg",
            PackSize = 20,
            Price = 15.00,
            Barcode = (string?)null,
            ManufacturerAr = (string?)null,
            ManufacturerEn = (string?)null,
            Source = 2, // Manual
        });
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var drug = JsonSerializer.Deserialize<DrugIdResponse>(body, JsonOptions);
        drug.ShouldNotBeNull();
        return drug.Id;
    }

    private async Task<BatchResponse> ReceiveBatchAsync(Guid branchId, Guid drugId, string batchNumber, int quantity)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/inventory", new
        {
            BranchId = branchId,
            DrugId = drugId,
            BatchNumber = batchNumber,
            ExpiryDate = "2027-12-31",
            Quantity = quantity,
            CostPrice = 10.00,
            SellingPrice = (decimal?)null,
        });
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var batch = JsonSerializer.Deserialize<BatchResponse>(body, JsonOptions);
        batch.ShouldNotBeNull();
        return batch;
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
        public bool IsMain { get; set; }
    }

    private sealed class DrugIdResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class BatchResponse
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public Guid DrugId { get; set; }
        public string DrugTradeNameEn { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int QuantityReceived { get; set; }
        public int QuantityOnHand { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
    }

    private sealed class PageResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
