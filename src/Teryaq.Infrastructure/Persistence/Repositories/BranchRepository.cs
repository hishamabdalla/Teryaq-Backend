namespace Teryaq.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Teryaq.Domain.Features.Branches;

/// <summary>EF Core implementation of <see cref="IBranchRepository"/>.</summary>
public sealed class BranchRepository : GenericRepository<Branch>, IBranchRepository
{
    /// <summary>Initialises a new instance of <see cref="BranchRepository"/>.</summary>
    public BranchRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct = default) =>
        await Context.Set<Branch>().AsNoTracking()
            .AnyAsync(b =>
                EF.Functions.Like(b.Name, name) &&
                (excludeId == null || b.Id != excludeId.Value),
                ct);
}
