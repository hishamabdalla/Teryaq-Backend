namespace Teryaq.Application.Features.Auth.Dtos;

/// <summary>Summarises the authenticated user embedded in every <see cref="AuthResponse"/>.</summary>
/// <param name="Id">Unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Name">Display name; falls back to the email address when no name has been set.</param>
/// <param name="Role">The user's current role (e.g. Owner, Pharmacist).</param>
/// <param name="TenantId">Tenant the user belongs to.</param>
/// <param name="BranchId">Branch the user is assigned to, or <see langword="null"/> for owner-level users.</param>
public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string Name,
    string Role,
    Guid TenantId,
    Guid? BranchId);
