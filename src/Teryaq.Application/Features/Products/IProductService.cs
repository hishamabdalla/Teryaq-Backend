namespace Teryaq.Application.Features.Products;

using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Products.Dtos;

/// <summary>Business operations for the product resource.</summary>
public interface IProductService
{
    /// <summary>Returns a single product, or a <c>NotFound</c> error if it does not exist.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a paginated list of all products.</summary>
    /// <param name="pagination">Page number and size.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<PaginatedList<ProductDto>>> GetAllAsync(PaginationParams pagination, CancellationToken ct = default);

    /// <summary>Validates and persists a new product.</summary>
    /// <param name="request">Creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    /// <summary>Validates and fully replaces an existing product.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="request">Replacement payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>Removes a product, or returns a <c>NotFound</c> error if it does not exist.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
