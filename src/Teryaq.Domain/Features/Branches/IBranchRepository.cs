namespace Teryaq.Domain.Features.Branches;

using Teryaq.Domain.Interfaces;

/// <summary>Repository contract for <see cref="Branch"/> aggregates.</summary>
public interface IBranchRepository : IRepository<Branch>
{
    /// <summary>Returns <see langword="true"/> if a branch with the given name already exists in the current tenant, excluding an optional record.</summary>
    /// <param name="name">Branch name to check (case-insensitive).</param>
    /// <param name="excludeId">Identifier to exclude from the check, used during updates.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct = default);
}
