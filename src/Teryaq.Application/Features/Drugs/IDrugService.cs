namespace Teryaq.Application.Features.Drugs;

using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Drugs.Dtos;
using Teryaq.Domain.Features.Drugs;

/// <summary>Business operations on the global Drug catalog.</summary>
public interface IDrugService
{
    /// <summary>Returns a paginated, filtered slice of the catalog.</summary>
    /// <param name="search">Optional substring matched against trade names and barcode.</param>
    /// <param name="source">Optional source filter.</param>
    /// <param name="isActive">Optional active-status filter.</param>
    /// <param name="pagination">Page number and size.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<PaginatedList<DrugDto>>> GetPagedAsync(
        string? search,
        DrugSource? source,
        bool? isActive,
        PaginationParams pagination,
        CancellationToken ct = default);

    /// <summary>Returns a single drug by its identifier.</summary>
    /// <param name="id">Drug identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<DrugDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new drug in the catalog.</summary>
    /// <param name="request">Creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<DrugDto>> CreateAsync(CreateDrugRequest request, CancellationToken ct = default);

    /// <summary>Fully replaces a drug's mutable fields.</summary>
    /// <param name="id">Drug identifier.</param>
    /// <param name="request">Update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<DrugDto>> UpdateAsync(Guid id, UpdateDrugRequest request, CancellationToken ct = default);

    /// <summary>Soft-deletes a drug from the catalog.</summary>
    /// <param name="id">Drug identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
