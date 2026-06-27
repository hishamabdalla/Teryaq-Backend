namespace Teryaq.Application.Features.Inventory;

using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Inventory.Dtos;

/// <summary>Business operations on stock batches held in branch inventories.</summary>
public interface IStockBatchService
{
    /// <summary>Returns a paginated, filtered slice of stock batches for the current tenant.</summary>
    /// <param name="branchId">Optional branch filter.</param>
    /// <param name="drugId">Optional drug filter.</param>
    /// <param name="expiringBefore">Optional upper-bound on expiry date — returns batches expiring on or before this date.</param>
    /// <param name="search">Optional substring matched against drug trade names or batch number.</param>
    /// <param name="pagination">Page number and size.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<PaginatedList<StockBatchDto>>> GetPagedAsync(
        Guid? branchId,
        Guid? drugId,
        DateOnly? expiringBefore,
        string? search,
        PaginationParams pagination,
        CancellationToken ct = default);

    /// <summary>Returns a single stock batch by its identifier.</summary>
    /// <param name="id">Batch identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<StockBatchDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Records a new stock batch received from a supplier.</summary>
    /// <param name="request">Receive payload including branch, drug, lot details, and quantities.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<StockBatchDto>> ReceiveAsync(ReceiveStockRequest request, CancellationToken ct = default);

    /// <summary>Applies a correction to an existing batch (quantity, price, or expiry date).</summary>
    /// <param name="id">Batch identifier.</param>
    /// <param name="request">Adjustment payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<StockBatchDto>> AdjustAsync(Guid id, AdjustStockRequest request, CancellationToken ct = default);

    /// <summary>Soft-deletes (writes off) a stock batch.</summary>
    /// <param name="id">Batch identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
