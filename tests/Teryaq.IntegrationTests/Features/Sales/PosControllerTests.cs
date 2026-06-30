namespace Teryaq.IntegrationTests.Features.Sales;

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

/// <summary>Integration tests for the <c>/api/v1/pos/sales</c> endpoint.</summary>
[Collection(IntegrationTestSuite.Name)]
public sealed class PosControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Initialises a new instance of <see cref="PosControllerTests"/>.</summary>
    public PosControllerTests(CustomWebApplicationFactory factory)
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
    // POST /api/v1/pos/sales
    // -------------------------------------------------------------------------

    /// <summary>An unauthenticated POST must return 401.</summary>
    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/pos/sales", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>A valid sale request returns 201, decrements the batch, and writes the receipt.</summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201AndDecrementsBatch()
    {
        string token = await RegisterAndGetTokenAsync("POS Pharmacy", "Main Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        var batch = await ReceiveBatchAsync(branchId, drugId, "LOT-POS-001", quantity: 10, sellingPrice: 20m);

        var salePayload = new
        {
            BranchId = branchId,
            Items = new[] { new { DrugId = drugId, Quantity = 3 } },
            Discount = 0.00,
            PaymentMethod = "Cash",
            CustomerId = (Guid?)null,
        };

        var response = await _client.PostAsJsonAsync("/api/v1/pos/sales", salePayload);

        string debugBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, debugBody);
        response.Headers.Location.ShouldNotBeNull();

        string body = await response.Content.ReadAsStringAsync();
        var sale = JsonSerializer.Deserialize<SaleResponse>(body, JsonOptions);
        sale.ShouldNotBeNull();
        sale.SaleNumber.ShouldStartWith("TRQ-");
        sale.GrandTotal.ShouldBe(60m); // 3 × 20
        sale.Lines.ShouldHaveSingleItem();
        sale.Lines[0].Quantity.ShouldBe(3);
        sale.Lines[0].BatchId.ShouldBe(batch.Id);

        // Verify stock was decremented.
        var batchResponse = await _client.GetAsync($"/api/v1/inventory/{batch.Id}");
        batchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        string batchBody = await batchResponse.Content.ReadAsStringAsync();
        var updatedBatch = JsonSerializer.Deserialize<BatchResponse>(batchBody, JsonOptions);
        updatedBatch.ShouldNotBeNull();
        updatedBatch.QuantityOnHand.ShouldBe(7);
    }

    /// <summary>Requesting more stock than is available returns 409 Conflict.</summary>
    [Fact]
    public async Task Create_InsufficientStock_Returns409()
    {
        string token = await RegisterAndGetTokenAsync("POS Insufficient", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        await ReceiveBatchAsync(branchId, drugId, "LOT-SMALL", quantity: 2, sellingPrice: 15m);

        var salePayload = new
        {
            BranchId = branchId,
            Items = new[] { new { DrugId = drugId, Quantity = 10 } },
            Discount = 0.00,
            PaymentMethod = "Cash",
            CustomerId = (Guid?)null,
        };

        var response = await _client.PostAsJsonAsync("/api/v1/pos/sales", salePayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/pos/sales/{id}
    // -------------------------------------------------------------------------

    /// <summary>A created sale can be retrieved by its identifier.</summary>
    [Fact]
    public async Task GetById_ExistingSale_Returns200WithLines()
    {
        string token = await RegisterAndGetTokenAsync("Get Sale Pharm", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        await ReceiveBatchAsync(branchId, drugId, "LOT-GET", quantity: 5, sellingPrice: 10m);

        var saleId = await CreateSaleAndGetIdAsync(branchId, drugId, 2);

        var response = await _client.GetAsync($"/api/v1/pos/sales/{saleId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var sale = JsonSerializer.Deserialize<SaleResponse>(body, JsonOptions);
        sale.ShouldNotBeNull();
        sale.Id.ShouldBe(saleId);
        sale.Lines.ShouldHaveSingleItem();
    }

    /// <summary>Requesting a non-existent sale returns 404.</summary>
    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        string token = await RegisterAndGetTokenAsync("Not Found Pharm", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/pos/sales/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/pos/sales/today
    // -------------------------------------------------------------------------

    /// <summary>Today's list includes a sale created moments ago.</summary>
    [Fact]
    public async Task GetTodays_IncludesRecentSale()
    {
        string token = await RegisterAndGetTokenAsync("Today Pharmacy", "Branch");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        await ReceiveBatchAsync(branchId, drugId, "LOT-TODAY", quantity: 5, sellingPrice: 10m);
        var saleId = await CreateSaleAndGetIdAsync(branchId, drugId, 1);

        var response = await _client.GetAsync("/api/v1/pos/sales/today");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<TodaySaleResponse>>(body, JsonOptions);
        list.ShouldNotBeNull();
        list.ShouldContain(s => s.Id == saleId);
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    /// <summary>Tenant A's sales are not visible to Tenant B.</summary>
    [Fact]
    public async Task TenantIsolation_TenantBCannotSeeTenantASales()
    {
        // Tenant A
        string tokenA = await RegisterAndGetTokenAsync("Tenant A POS", "Branch A");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var branchIdA = await GetMainBranchIdAsync();
        var drugIdA = await CreateDrugAndGetIdAsync();
        await ReceiveBatchAsync(branchIdA, drugIdA, "LOT-A", quantity: 5, sellingPrice: 10m);
        var saleIdA = await CreateSaleAndGetIdAsync(branchIdA, drugIdA, 1);

        // Tenant B
        string tokenB = await RegisterAndGetTokenAsync("Tenant B POS", "Branch B");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync($"/api/v1/pos/sales/{saleIdA}");
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
        var drug = JsonSerializer.Deserialize<IdResponse>(body, JsonOptions);
        drug.ShouldNotBeNull();
        return drug.Id;
    }

    private async Task<BatchResponse> ReceiveBatchAsync(Guid branchId, Guid drugId, string batchNumber, int quantity, decimal sellingPrice)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/inventory", new
        {
            BranchId = branchId,
            DrugId = drugId,
            BatchNumber = batchNumber,
            ExpiryDate = "2027-12-31",
            Quantity = quantity,
            ReorderLevel = 0,
            CostPrice = sellingPrice * 0.6m,
            SellingPrice = sellingPrice,
        });
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var batch = JsonSerializer.Deserialize<BatchResponse>(body, JsonOptions);
        batch.ShouldNotBeNull();
        return batch;
    }

    private async Task<Guid> CreateSaleAndGetIdAsync(Guid branchId, Guid drugId, int quantity)
    {
        var payload = new
        {
            BranchId = branchId,
            Items = new[] { new { DrugId = drugId, Quantity = quantity } },
            Discount = 0.00,
            PaymentMethod = "Cash",
            CustomerId = (Guid?)null,
        };
        var response = await _client.PostAsJsonAsync("/api/v1/pos/sales", payload);
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var sale = JsonSerializer.Deserialize<IdResponse>(body, JsonOptions);
        sale.ShouldNotBeNull();
        return sale.Id;
    }

    // -------------------------------------------------------------------------
    // Local response models
    // -------------------------------------------------------------------------

    private sealed class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class BranchResponse
    {
        public Guid Id { get; set; }
        public bool IsMain { get; set; }
    }

    private sealed class IdResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class BatchResponse
    {
        public Guid Id { get; set; }
        public int QuantityOnHand { get; set; }
    }

    private sealed class SaleLineResponse
    {
        public Guid BatchId { get; set; }
        public int Quantity { get; set; }
    }

    private sealed class SaleResponse
    {
        public Guid Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public List<SaleLineResponse> Lines { get; set; } = [];
    }

    private sealed class TodaySaleResponse
    {
        public Guid Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
    }
}
