namespace Teryaq.Application.Features.Users.Dtos;

/// <summary>Payload for updating the mutable details of an existing user.</summary>
/// <param name="FullName">New display name for the user.</param>
/// <param name="BranchId">New branch assignment, or <see langword="null"/> to clear the branch assignment.</param>
public sealed record UpdateUserRequest(
    string FullName,
    Guid? BranchId);
