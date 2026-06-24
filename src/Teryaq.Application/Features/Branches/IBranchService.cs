namespace Teryaq.Application.Features.Branches;

using Teryaq.Application.Common;
using Teryaq.Application.Features.Branches.Dtos;

/// <summary>Manages branches belonging to the current authenticated tenant.</summary>
public interface IBranchService
{
    /// <summary>Returns all branches visible to the current tenant.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<BranchDto>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single branch by its identifier.</summary>
    /// <param name="id">Branch identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<BranchDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new branch under the current tenant.</summary>
    /// <param name="request">Branch creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<BranchDto>> CreateAsync(CreateBranchRequest request, CancellationToken ct = default);

    /// <summary>Updates the mutable fields of an existing branch.</summary>
    /// <param name="id">Branch identifier.</param>
    /// <param name="request">Update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<BranchDto>> UpdateAsync(Guid id, UpdateBranchRequest request, CancellationToken ct = default);

    /// <summary>Soft-deletes the branch.</summary>
    /// <param name="id">Branch identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deactivates the branch, preventing it from being used for new operations.</summary>
    /// <param name="id">Branch identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<BranchDto>> DeactivateAsync(Guid id, CancellationToken ct = default);
}
