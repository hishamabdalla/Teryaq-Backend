namespace Teryaq.Application.Features.Branches.Dtos;

/// <summary>Payload for updating the mutable fields of an existing branch.</summary>
/// <param name="Name">New display name (must remain unique within the tenant).</param>
/// <param name="Address">New street address, or <see langword="null"/> to clear.</param>
/// <param name="Phone">New contact phone number, or <see langword="null"/> to clear.</param>
public sealed record UpdateBranchRequest(
    string Name,
    string? Address,
    string? Phone);
