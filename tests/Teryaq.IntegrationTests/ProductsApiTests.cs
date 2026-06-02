namespace Teryaq.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Products.Dtos;
using Teryaq.Infrastructure.Persistence;
using Shouldly;
using Xunit;

public sealed class ProductsApiTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_Products_Returns201_WithValidRequest()
    {
        var request = new CreateProductRequest("Test Widget", "A test widget", 19.99m);

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        dto.ShouldNotBeNull();
        dto!.Name.ShouldBe("Test Widget");
        response.Headers.Location.ShouldNotBeNull();
    }

    [Fact]
    public async Task GET_Products_ById_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync(new Uri($"/api/v1/products/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Products_Returns422_WithInvalidRequest()
    {
        var request = new CreateProductRequest(string.Empty, null, -1m);

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GET_Products_ReturnsPaginatedList()
    {
        var createRequest = new CreateProductRequest("Paginated Widget", null, 5m);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await _client.GetAsync(new Uri("/api/v1/products?pageNumber=1&pageSize=10", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PaginatedList<ProductDto>>();
        paged.ShouldNotBeNull();
        paged!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task PUT_Products_Returns200_WhenProductExists()
    {
        var created = await CreateProductAsync("Original", null, 10m);

        var updateRequest = new UpdateProductRequest("Updated", "New desc", 20m);
        var response = await _client.PutAsJsonAsync(new Uri($"/api/v1/products/{created.Id}", UriKind.Relative), updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        dto.ShouldNotBeNull();
        dto!.Name.ShouldBe("Updated");
    }

    [Fact]
    public async Task PUT_Products_Returns404_WhenProductDoesNotExist()
    {
        var updateRequest = new UpdateProductRequest("Name", null, 1m);
        var response = await _client.PutAsJsonAsync(new Uri($"/api/v1/products/{Guid.NewGuid()}", UriKind.Relative), updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Products_Returns204_WhenProductExists()
    {
        var created = await CreateProductAsync("ToDelete", null, 1m);

        var response = await _client.DeleteAsync(new Uri($"/api/v1/products/{created.Id}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Products_SoftDeletes_VerifiedViaIgnoreQueryFilters()
    {
        var created = await CreateProductAsync("SoftDeleteMe", null, 1m);

        await _client.DeleteAsync(new Uri($"/api/v1/products/{created.Id}", UriKind.Relative));

        var getAfterDelete = await _client.GetAsync(new Uri($"/api/v1/products/{created.Id}", UriKind.Relative));
        getAfterDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deletedRow = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == created.Id);

        deletedRow.ShouldNotBeNull();
        deletedRow!.IsDeleted.ShouldBeTrue();
        deletedRow.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task DELETE_Products_Returns404_WhenProductDoesNotExist()
    {
        var response = await _client.DeleteAsync(new Uri($"/api/v1/products/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<ProductDto> CreateProductAsync(string name, string? description, decimal price)
    {
        var request = new CreateProductRequest(name, description, price);
        var response = await _client.PostAsJsonAsync("/api/v1/products", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
}
