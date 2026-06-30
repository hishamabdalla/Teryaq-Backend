namespace Teryaq.Domain.Features.Sales;

using Teryaq.Domain.Interfaces;

/// <summary>Repository contract for <see cref="Sale"/> aggregates.</summary>
public interface ISaleRepository : IRepository<Sale>
{
    /// <summary>
    /// Returns a <see cref="Sale"/> by identifier with all navigation properties eagerly loaded
    /// (<c>Lines → Drug</c>, <c>Lines → Batch</c>, <c>Customer</c>).
    /// </summary>
    /// <param name="id">Sale identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Sale?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all completed sales whose <see cref="Sale.CompletedAt"/> falls on <paramref name="today"/>, ordered by <see cref="Sale.CompletedAt"/> descending.</summary>
    /// <param name="branchId">Optional branch filter; when <see langword="null"/> all branches are included.</param>
    /// <param name="today">The calendar day to filter on.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<Sale>> GetTodaysSalesAsync(Guid? branchId, DateOnly today, CancellationToken ct = default);
}
