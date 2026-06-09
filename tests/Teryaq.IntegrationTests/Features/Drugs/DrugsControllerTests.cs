namespace Teryaq.IntegrationTests.Features.Drugs;

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

/// <summary>Integration tests for the <c>/api/v1/drugs</c> endpoint covering all CRUD operations and auth enforcement.</summary>
public sealed class DrugsControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Initialises a new instance of <see cref="DrugsControllerTests"/>.</summary>
    public DrugsControllerTests(CustomWebApplicationFactory factory)
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
    // GET /api/v1/drugs
    // -------------------------------------------------------------------------

    /// <summary>An unauthenticated request to the paged endpoint must return 401.</summary>
    [Fact]
    public async Task GetPaged_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/drugs");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>An authenticated Owner with no drugs in the catalog must receive an empty page with 200 OK.</summary>
    [Fact]
    public async Task GetPaged_AuthenticatedOwner_ReturnsOkAndEmptyPage()
    {
        string token = await RegisterOwnerAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/drugs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string body = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<PagedResponse<DrugResponse>>(body, JsonOptions);

        page.ShouldNotBeNull();
        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/drugs
    // -------------------------------------------------------------------------

    /// <summary>Creating a drug with a valid payload must return 201 with a Location header.</summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        string token = await RegisterOwnerAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/drugs", BuildCreateRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var drug = await DeserializeDrugAsync(response);
        drug.ShouldNotBeNull();
        drug.TradeNameEn.ShouldBe("Amoxil");
    }

    /// <summary>Creating a drug whose TradeNameEn + Strength + DosageForm already exists must return 409.</summary>
    [Fact]
    public async Task Create_DuplicateDrug_Returns409()
    {
        string token = await RegisterOwnerAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        object request = BuildCreateRequest();
        await _client.PostAsJsonAsync("/api/v1/drugs", request);

        var duplicate = await _client.PostAsJsonAsync("/api/v1/drugs", request);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/drugs/{id}
    // -------------------------------------------------------------------------

    /// <summary>Fetching an existing drug by its identifier must return the matching DTO with 200 OK.</summary>
    [Fact]
    public async Task GetById_ExistingDrug_ReturnsDrugDto()
    {
        string token = await RegisterOwnerAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await CreateDrugAsync();
        var response = await _client.GetAsync($"/api/v1/drugs/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var drug = await DeserializeDrugAsync(response);
        drug.ShouldNotBeNull();
        drug.Id.ShouldBe(created.Id);
        drug.TradeNameEn.ShouldBe(created.TradeNameEn);
    }

    /// <summary>Fetching a drug that does not exist must return 404.</summary>
    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        string token = await RegisterOwnerAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/drugs/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/drugs/{id}
    // -------------------------------------------------------------------------

    /// <summary>Updating an existing drug with valid data must return the updated DTO with 200 OK.</summary>
    [Fact]
    public async Task Update_ExistingDrug_Returns200()
    {
        string token = await RegisterOwnerAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await CreateDrugAsync();

        var updateRequest = new
        {
            TradeNameAr = "أموكسيسيلين محدث",
            TradeNameEn = "Amoxil Updated",
            GenericName = "Amoxicillin",
            DosageForm = "Capsule",
            Strength = "500mg",
            PackSize = 20,
            Price = 45.00m,
            Barcode = (string?)null,
            ManufacturerAr = (string?)null,
            ManufacturerEn = "GSK Updated",
            IsActive = true,
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/drugs/{created.Id}", updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var drug = await DeserializeDrugAsync(response);
        drug.ShouldNotBeNull();
        drug.TradeNameEn.ShouldBe("Amoxil Updated");
        drug.ManufacturerEn.ShouldBe("GSK Updated");
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/drugs/{id}
    // -------------------------------------------------------------------------

    /// <summary>Deleting an existing drug must return 204 No Content.</summary>
    [Fact]
    public async Task Delete_ExistingDrug_Returns204()
    {
        string token = await RegisterOwnerAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var created = await CreateDrugAsync();

        var response = await _client.DeleteAsync($"/api/v1/drugs/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<string> RegisterOwnerAndGetTokenAsync()
    {
        var payload = new
        {
            PharmacyName = "Test Pharmacy",
            BranchName = "Main Branch",
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
        auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        return auth.AccessToken;
    }

    private static object BuildCreateRequest() => new
    {
        TradeNameAr = "أموكسيسيلين",
        TradeNameEn = "Amoxil",
        GenericName = "Amoxicillin",
        DosageForm = "Capsule",
        Strength = "500mg",
        PackSize = 20,
        Price = 42.50m,
        Barcode = (string?)null,
        ManufacturerAr = (string?)null,
        ManufacturerEn = "GSK",
        Source = 2, // DrugSource.Manual
    };

    private async Task<DrugResponse> CreateDrugAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/drugs", BuildCreateRequest());
        response.EnsureSuccessStatusCode();
        var drug = await DeserializeDrugAsync(response);
        drug.ShouldNotBeNull();
        return drug;
    }

    private static async Task<DrugResponse?> DeserializeDrugAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DrugResponse>(body, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Local response models (mirror API shapes for deserialization)
    // -------------------------------------------------------------------------

    private sealed class AuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class DrugResponse
    {
        public Guid Id { get; set; }
        public string TradeNameAr { get; set; } = string.Empty;
        public string TradeNameEn { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public int PackSize { get; set; }
        public decimal Price { get; set; }
        public string? Barcode { get; set; }
        public string? ManufacturerAr { get; set; }
        public string? ManufacturerEn { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class PagedResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
