namespace Teryaq.Application.Features.Sales;

using Teryaq.Application.Common;
using Teryaq.Application.Features.Sales.Dtos;

/// <summary>Processes POS sale transactions with FEFO stock dispensing and audit-trail recording.</summary>
public interface ISaleService
{
    /// <summary>Confirms a POS sale: performs FEFO stock allocation, decrements batch quantities, writes <c>StockMovement</c> audit records, and persists the sale atomically.</summary>
    /// <param name="request">Sale details including line items, discount, and payment method.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="SaleDto"/> on success, or a failure result on validation, insufficient stock, or missing data.</returns>
    Task<Result<SaleDto>> CreateAsync(CreateSaleRequest request, CancellationToken ct = default);

    /// <summary>Returns a sale by identifier with all line items, or a not-found error.</summary>
    /// <param name="id">Sale identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<SaleDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a summary list of all sales completed today, optionally filtered by branch.</summary>
    /// <param name="branchId">Optional branch filter; when <see langword="null"/> all branches for the tenant are included.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<List<TodaySaleSummaryDto>>> GetTodaysAsync(Guid? branchId, CancellationToken ct = default);
}
