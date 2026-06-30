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

    /// <summary>
    /// Returns all non-empty batches whose <see cref="StockBatch.ExpiryDate"/> is on or before
    /// <paramref name="expiringOnOrBefore"/>, with <c>Drug</c> and <c>Branch</c> navigation properties
    /// eagerly loaded. Results are ordered by <see cref="StockBatch.ExpiryDate"/> ascending (most urgent first).
    /// </summary>
    /// <param name="branchId">Optional branch filter; when <see langword="null"/> all branches are included.</param>
    /// <param name="expiringOnOrBefore">Upper-bound expiry date (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<StockBatch>> GetNearExpiryAsync(Guid? branchId, DateOnly expiringOnOrBefore, CancellationToken ct = default);

    /// <summary>
    /// Returns all batches where <see cref="StockBatch.ReorderLevel"/> is greater than zero and
    /// <see cref="StockBatch.QuantityOnHand"/> is at or below <see cref="StockBatch.ReorderLevel"/>,
    /// with <c>Drug</c> and <c>Branch</c> navigation properties eagerly loaded.
    /// Results are ordered by <see cref="StockBatch.QuantityOnHand"/> ascending (most critical first).
    /// </summary>
    /// <param name="branchId">Optional branch filter; when <see langword="null"/> all branches are included.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<StockBatch>> GetLowStockAsync(Guid? branchId, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-expired, non-empty batches for the given drug at the given branch,
    /// <em>with change-tracking enabled</em> so that callers can call <see cref="StockBatch.Dispense"/>
    /// and the changes are persisted on the next <c>SaveChanges</c>. Results are ordered by
    /// <see cref="StockBatch.ExpiryDate"/> ascending (FEFO — first-expired-first-out).
    /// </summary>
    /// <param name="branchId">Branch to dispense from.</param>
    /// <param name="drugId">Drug to dispense.</param>
    /// <param name="today">Today's date; batches expiring on or before this date are excluded.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<StockBatch>> GetDispensableBatchesAsync(Guid branchId, Guid drugId, DateOnly today, CancellationToken ct = default);
}
