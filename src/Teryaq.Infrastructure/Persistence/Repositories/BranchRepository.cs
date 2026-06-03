namespace Teryaq.Infrastructure.Persistence.Repositories;

using Teryaq.Domain.Features.Branches;

/// <summary>EF Core implementation of <see cref="IBranchRepository"/>.</summary>
public sealed class BranchRepository : GenericRepository<Branch>, IBranchRepository
{
    /// <summary>Initialises a new instance of <see cref="BranchRepository"/>.</summary>
    public BranchRepository(AppDbContext context) : base(context) { }
}
