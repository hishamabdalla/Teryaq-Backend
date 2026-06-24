namespace Teryaq.Application.Features.Branches.Dtos;

/// <summary>Read model returned by all branch endpoints.</summary>
/// <param name="Id">Unique identifier of the branch.</param>
/// <param name="Name">Display name of the branch.</param>
/// <param name="Address">Street address, or <see langword="null"/> when not set.</param>
/// <param name="Phone">Contact phone number, or <see langword="null"/> when not set.</param>
/// <param name="IsMain">Whether this is the tenant's primary branch.</param>
/// <param name="IsActive">Whether this branch is currently operational.</param>
public sealed record BranchDto(
    Guid Id,
    string Name,
    string? Address,
    string? Phone,
    bool IsMain,
    bool IsActive);
