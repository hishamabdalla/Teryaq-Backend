namespace Teryaq.Domain.Features.Inventory;

using Teryaq.Domain.Interfaces;

/// <summary>Repository contract for <see cref="StockBatch"/> aggregates.</summary>
public interface IStockBatchRepository : IRepository<StockBatch>
{
    /// <summary>
    /// Returns a <see cref="StockBatch"/> by identifier with <c>Drug</c> and <c>Branch</c> navigation
    /// properties eagerly loaded, without change-tracking. Use this for read responses.
    /// </summary>
    /// <param name="id">Batch identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<StockBatch?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns a filtered, paginated slice of stock batches with <c>Drug</c> and <c>Branch</c>
    /// navigation properties loaded, ordered by <see cref="StockBatch.ExpiryDate"/> ascending (FEFO order).
    /// </summary>
    /// <param name="branchId">Optional branch filter.</param>
    /// <param name="drugId">Optional drug filter.</param>
    /// <param name="expiringBefore">Optional upper-bound on expiry date (inclusive).</param>
    /// <param name="search">Optional substring matched against drug trade names or batch number.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<(IReadOnlyList<StockBatch> Items, int TotalCount)> SearchAsync(
        Guid? branchId,
        Guid? drugId,
        DateOnly? expiringBefore,
        string? search,
        int skip,
        int take,
        CancellationToken ct = default);
}
