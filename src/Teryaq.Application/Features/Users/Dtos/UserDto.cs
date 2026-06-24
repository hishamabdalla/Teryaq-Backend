namespace Teryaq.Application.Features.Users.Dtos;

/// <summary>Read model returned by all user management endpoints.</summary>
/// <param name="Id">Unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="FullName">Display name of the user.</param>
/// <param name="Role">Role assigned to the user (Owner or Pharmacist).</param>
/// <param name="BranchId">Branch the user is assigned to, or <see langword="null"/> for owner-level users.</param>
/// <param name="IsLocked">Whether the user account is currently locked out.</param>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? BranchId,
    bool IsLocked);
