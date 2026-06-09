namespace Teryaq.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Teryaq.Domain.Features.Drugs;

/// <summary>EF Core implementation of <see cref="IDrugRepository"/>.</summary>
public sealed class DrugRepository : GenericRepository<Drug>, IDrugRepository
{
    /// <summary>Initialises a new instance of <see cref="DrugRepository"/>.</summary>
    public DrugRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Drug?> GetByBarcodeAsync(string barcode, CancellationToken ct = default) =>
        await Context.Set<Drug>().AsNoTracking()
            .FirstOrDefaultAsync(d => d.Barcode == barcode, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAndFormAsync(
        string tradeNameEn,
        string strength,
        string dosageForm,
        Guid? excludeId,
        CancellationToken ct = default) =>
        await Context.Set<Drug>().AsNoTracking()
            .AnyAsync(d =>
                EF.Functions.Like(d.TradeNameEn, tradeNameEn) &&
                EF.Functions.Like(d.Strength, strength) &&
                EF.Functions.Like(d.DosageForm, dosageForm) &&
                (excludeId == null || d.Id != excludeId.Value),
                ct);

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Drug> Items, int TotalCount)> SearchAsync(
        string? search,
        DrugSource? source,
        bool? isActive,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = Context.Set<Drug>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search}%";
            query = query.Where(d =>
                EF.Functions.Like(d.TradeNameAr, pattern) ||
                EF.Functions.Like(d.TradeNameEn, pattern) ||
                (d.Barcode != null && EF.Functions.Like(d.Barcode, pattern)));
        }

        if (source.HasValue)
            query = query.Where(d => d.Source == source.Value);

        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
