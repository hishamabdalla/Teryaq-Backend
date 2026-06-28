namespace Teryaq.IntegrationTests.Features.Alerts;

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

/// <summary>Integration tests for the <c>/api/v1/alerts</c> endpoint covering near-expiry, low-stock, and tenant isolation.</summary>
[Collection(IntegrationTestSuite.Name)]
public sealed class AlertsControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Initialises a new instance of <see cref="AlertsControllerTests"/>.</summary>
    public AlertsControllerTests(CustomWebApplicationFactory factory)
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
    // Auth
    // -------------------------------------------------------------------------

    /// <summary>An unauthenticated request must return 401.</summary>
    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/alerts");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Authenticated staff with no flagged batches receives an empty list — not an error.</summary>
    [Fact]
    public async Task GetAll_NoAlerts_ReturnsEmptyList()
    {
        string token = await RegisterAndGetTokenAsync("Alert Empty Pharmacy", "Alert Empty Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/alerts");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var alerts = JsonSerializer.Deserialize<List<AlertResponse>>(body, JsonOptions);
        alerts.ShouldNotBeNull();
        alerts.ShouldBeEmpty();
    }

    // -------------------------------------------------------------------------
    // Near-expiry alerts
    // -------------------------------------------------------------------------

    /// <summary>A batch expiring within 90 days appears as a NearExpiry alert with correct DaysUntilExpiry.</summary>
    [Fact]
    public async Task GetAll_NearExpiryBatch_ReturnsNearExpiryAlert()
    {
        string token = await RegisterAndGetTokenAsync("NearExpiry Pharmacy", "NearExpiry Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();

        // Batch expires in 30 days — within the 90-day window
        string expiryDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        await ReceiveBatchAsync(branchId, drugId, "LOT-EXPIRING", 50, 0, expiryDate);

        var response = await _client.GetAsync("/api/v1/alerts");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var alerts = JsonSerializer.Deserialize<List<AlertResponse>>(body, JsonOptions);
        alerts.ShouldNotBeNull();

        var nearExpiryAlerts = alerts.Where(a => a.Type == "NearExpiry").ToList();
        nearExpiryAlerts.ShouldNotBeEmpty();
        nearExpiryAlerts[0].DaysUntilExpiry.ShouldNotBeNull();
        nearExpiryAlerts[0].DaysUntilExpiry!.Value.ShouldBeInRange(28, 32); // ±2 days for test timing
        nearExpiryAlerts[0].BatchNumber.ShouldBe("LOT-EXPIRING");
        nearExpiryAlerts[0].Severity.ShouldBe("High"); // 30 days ≤ NearExpiryHighDays(30)
    }

    /// <summary>A batch expiring beyond 90 days does NOT appear in the alerts list.</summary>
    [Fact]
    public async Task GetAll_BatchNotNearExpiry_IsNotIncluded()
    {
        string token = await RegisterAndGetTokenAsync("FarExpiry Pharmacy", "FarExpiry Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();

        // Batch expires in 200 days — outside the 90-day window
        string expiryDate = DateTime.UtcNow.AddDays(200).ToString("yyyy-MM-dd");
        await ReceiveBatchAsync(branchId, drugId, "LOT-FAR", 50, 0, expiryDate);

        var response = await _client.GetAsync("/api/v1/alerts");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var alerts = JsonSerializer.Deserialize<List<AlertResponse>>(body, JsonOptions);
        alerts.ShouldNotBeNull();
        alerts.ShouldNotContain(a => a.BatchNumber == "LOT-FAR");
    }

    // -------------------------------------------------------------------------
    // Low-stock alerts
    // -------------------------------------------------------------------------

    /// <summary>A batch whose on-hand quantity is at or below its reorder level appears as a LowStock alert.</summary>
    [Fact]
    public async Task GetAll_LowStockBatch_ReturnsLowStockAlert()
    {
        string token = await RegisterAndGetTokenAsync("LowStock Pharmacy", "LowStock Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();

        // Receive 5 units with reorderLevel = 10 → immediately low-stock
        string expiryDate = DateTime.UtcNow.AddDays(365).ToString("yyyy-MM-dd");
        await ReceiveBatchAsync(branchId, drugId, "LOT-LOWSTOCK", 5, 10, expiryDate);

        var response = await _client.GetAsync("/api/v1/alerts");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var alerts = JsonSerializer.Deserialize<List<AlertResponse>>(body, JsonOptions);
        alerts.ShouldNotBeNull();

        var lowStockAlerts = alerts.Where(a => a.Type == "LowStock").ToList();
        lowStockAlerts.ShouldNotBeEmpty();
        lowStockAlerts[0].BatchNumber.ShouldBe("LOT-LOWSTOCK");
        lowStockAlerts[0].QuantityOnHand.ShouldBe(5);
        lowStockAlerts[0].ReorderLevel.ShouldBe(10);
        lowStockAlerts[0].DaysUntilExpiry.ShouldBeNull();
        lowStockAlerts[0].Severity.ShouldBe("Medium"); // 5/10 = 0.50 ≤ LowStockMediumRatio(0.50)
    }

    /// <summary>A batch with reorderLevel = 0 does NOT appear as a low-stock alert regardless of quantity.</summary>
    [Fact]
    public async Task GetAll_BatchWithZeroReorderLevel_IsNotLowStockAlert()
    {
        string token = await RegisterAndGetTokenAsync("NoReorder Pharmacy", "NoReorder Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();

        // reorderLevel = 0 → alert disabled
        string expiryDate = DateTime.UtcNow.AddDays(365).ToString("yyyy-MM-dd");
        await ReceiveBatchAsync(branchId, drugId, "LOT-NOREORDER", 1, 0, expiryDate);

        var response = await _client.GetAsync("/api/v1/alerts");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var alerts = JsonSerializer.Deserialize<List<AlertResponse>>(body, JsonOptions);
        alerts.ShouldNotBeNull();
        alerts.ShouldNotContain(a => a.Type == "LowStock" && a.BatchNumber == "LOT-NOREORDER");
    }

    // -------------------------------------------------------------------------
    // Type filter
    // -------------------------------------------------------------------------

    /// <summary>Filtering by type=NearExpiry returns only near-expiry alerts.</summary>
    [Fact]
    public async Task GetAll_TypeFilterNearExpiry_ReturnsOnlyNearExpiry()
    {
        string token = await RegisterAndGetTokenAsync("TypeFilter Pharmacy", "TypeFilter Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var branchId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        string nearExpiryDate = DateTime.UtcNow.AddDays(20).ToString("yyyy-MM-dd");
        string farExpiryDate = DateTime.UtcNow.AddDays(365).ToString("yyyy-MM-dd");

        await ReceiveBatchAsync(branchId, drugId, "LOT-NEAR", 50, 0, nearExpiryDate);
        await ReceiveBatchAsync(branchId, drugId, "LOT-LOWSTOCK2", 3, 20, farExpiryDate);

        var response = await _client.GetAsync("/api/v1/alerts?type=NearExpiry");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var alerts = JsonSerializer.Deserialize<List<AlertResponse>>(body, JsonOptions);
        alerts.ShouldNotBeNull();
        alerts.ShouldAllBe(a => a.Type == "NearExpiry");
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    /// <summary>Tenant A's alerts are invisible to Tenant B.</summary>
    [Fact]
    public async Task TenantIsolation_AlertsFromOtherTenant_AreNotVisible()
    {
        // Tenant A: receive a near-expiry batch
        string tokenA = await RegisterAndGetTokenAsync("Tenant A Alerts", "Tenant A Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var branchAId = await GetMainBranchIdAsync();
        var drugId = await CreateDrugAndGetIdAsync();
        string expiryDate = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd");
        var batchA = await ReceiveBatchAsync(branchAId, drugId, "LOT-TENANT-A-ALERT", 5, 0, expiryDate);

        // Tenant B: should see no alerts
        string tokenB = await RegisterAndGetTokenAsync("Tenant B Alerts", "Tenant B Main");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync("/api/v1/alerts");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        var alerts = JsonSerializer.Deserialize<List<AlertResponse>>(body, JsonOptions);
        alerts.ShouldNotBeNull();
        alerts.ShouldNotContain(a => a.StockBatchId == batchA.Id);
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

    private async Task<BatchIdResponse> ReceiveBatchAsync(
        Guid branchId,
        Guid drugId,
        string batchNumber,
        int quantity,
        int reorderLevel,
        string expiryDate)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/inventory", new
        {
            BranchId = branchId,
            DrugId = drugId,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            Quantity = quantity,
            ReorderLevel = reorderLevel,
            CostPrice = 10.00,
            SellingPrice = (decimal?)null,
        });
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        var batch = JsonSerializer.Deserialize<BatchIdResponse>(body, JsonOptions);
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

    private sealed class BatchIdResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class AlertResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public Guid StockBatchId { get; set; }
        public Guid BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public Guid DrugId { get; set; }
        public string DrugTradeNameEn { get; set; } = string.Empty;
        public string DrugTradeNameAr { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public int? DaysUntilExpiry { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
    }
}
