namespace Teryaq.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Teryaq.Domain.Features.Sales;

/// <summary>EF Core implementation of <see cref="ISaleRepository"/>.</summary>
public sealed class SaleRepository : GenericRepository<Sale>, ISaleRepository
{
    /// <summary>Initialises a new instance of <see cref="SaleRepository"/>.</summary>
    public SaleRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Sale?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        await Context.Set<Sale>()
            .Include(s => s.Branch)
            .Include(s => s.Customer)
            .Include(s => s.Lines).ThenInclude(l => l.Drug)
            .Include(s => s.Lines).ThenInclude(l => l.Batch)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Sale>> GetTodaysSalesAsync(
        Guid? branchId,
        DateOnly today,
        CancellationToken ct = default)
    {
        var query = Context.Set<Sale>()
            .Include(s => s.Lines)
            .Include(s => s.Customer)
            .AsNoTracking()
            .Where(s => DateOnly.FromDateTime(s.CompletedAt) == today);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        return await query
            .OrderByDescending(s => s.CompletedAt)
            .ToListAsync(ct);
    }
}
