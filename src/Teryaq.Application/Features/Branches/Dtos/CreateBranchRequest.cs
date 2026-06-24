namespace Teryaq.Application.Features.Branches.Dtos;

/// <summary>Payload for creating a new branch under the current tenant.</summary>
/// <param name="Name">Display name for the branch (must be unique within the tenant).</param>
/// <param name="Address">Optional street address.</param>
/// <param name="Phone">Optional contact phone number.</param>
public sealed record CreateBranchRequest(
    string Name,
    string? Address,
    string? Phone);
