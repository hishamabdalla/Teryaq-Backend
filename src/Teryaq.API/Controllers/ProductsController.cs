namespace Teryaq.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Teryaq.API.Controllers.Base;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Products;
using Teryaq.Application.Features.Products.Dtos;

/// <summary>Manages product resources.</summary>
public sealed class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    /// <summary>Initialises a new instance of <see cref="ProductsController"/>.</summary>
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Returns a paginated list of all products.</summary>
    /// <param name="pagination">Page number and page size.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType<PaginatedList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] PaginationParams pagination, CancellationToken ct) =>
        HandleResult(await _productService.GetAllAsync(pagination, ct));

    /// <summary>Returns a single product by its identifier.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct) =>
        HandleResult(await _productService.GetByIdAsync(id, ct));

    /// <summary>Creates a new product and returns it with a <c>Location</c> header.</summary>
    /// <param name="request">Product creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.CreateAsync(request, ct);
        return HandleCreated(result, nameof(GetByIdAsync), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>Fully replaces an existing product.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="request">Replacement payload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        HandleResult(await _productService.UpdateAsync(id, request, ct));

    /// <summary>Permanently deletes a product.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct) =>
        HandleDelete(await _productService.DeleteAsync(id, ct));
}
