namespace Teryaq.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Teryaq.Domain.Features.Inventory;

/// <summary>EF Core implementation of <see cref="IStockBatchRepository"/>.</summary>
public sealed class StockBatchRepository : GenericRepository<StockBatch>, IStockBatchRepository
{
    /// <summary>Initialises a new instance of <see cref="StockBatchRepository"/>.</summary>
    public StockBatchRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<StockBatch?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        await Context.Set<StockBatch>()
            .Include(b => b.Drug)
            .Include(b => b.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<StockBatch> Items, int TotalCount)> SearchAsync(
        Guid? branchId,
        Guid? drugId,
        DateOnly? expiringBefore,
        string? search,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = Context.Set<StockBatch>()
            .Include(b => b.Drug)
            .Include(b => b.Branch)
            .AsNoTracking();

        if (branchId.HasValue)
            query = query.Where(b => b.BranchId == branchId.Value);

        if (drugId.HasValue)
            query = query.Where(b => b.DrugId == drugId.Value);

        if (expiringBefore.HasValue)
            query = query.Where(b => b.ExpiryDate <= expiringBefore.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search}%";
            query = query.Where(b =>
                EF.Functions.Like(b.Drug.TradeNameEn, pattern) ||
                EF.Functions.Like(b.Drug.TradeNameAr, pattern) ||
                EF.Functions.Like(b.BatchNumber, pattern));
        }

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(b => b.ExpiryDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StockBatch>> GetNearExpiryAsync(
        Guid? branchId,
        DateOnly expiringOnOrBefore,
        CancellationToken ct = default)
    {
        var query = Context.Set<StockBatch>()
            .Include(b => b.Drug)
            .Include(b => b.Branch)
            .AsNoTracking()
            .Where(b => b.ExpiryDate <= expiringOnOrBefore && b.QuantityOnHand > 0);

        if (branchId.HasValue)
            query = query.Where(b => b.BranchId == branchId.Value);

        return await query
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StockBatch>> GetLowStockAsync(
        Guid? branchId,
        CancellationToken ct = default)
    {
        var query = Context.Set<StockBatch>()
            .Include(b => b.Drug)
            .Include(b => b.Branch)
            .AsNoTracking()
            .Where(b => b.ReorderLevel > 0 && b.QuantityOnHand <= b.ReorderLevel);

        if (branchId.HasValue)
            query = query.Where(b => b.BranchId == branchId.Value);

        return await query
            .OrderBy(b => b.QuantityOnHand)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<StockBatch>> GetDispensableBatchesAsync(
        Guid branchId,
        Guid drugId,
        DateOnly today,
        CancellationToken ct = default) =>
        await Context.Set<StockBatch>()
            .AsTracking()
            .Where(b => b.BranchId == branchId && b.DrugId == drugId && b.QuantityOnHand > 0 && b.ExpiryDate > today)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);
}
