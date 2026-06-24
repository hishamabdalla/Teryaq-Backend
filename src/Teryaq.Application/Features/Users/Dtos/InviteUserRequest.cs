namespace Teryaq.Application.Features.Users.Dtos;

/// <summary>Payload for inviting a new Pharmacist user to the current tenant.</summary>
/// <param name="Email">Email address the pharmacist will use to log in.</param>
/// <param name="FullName">Display name for the pharmacist.</param>
/// <param name="Password">Initial password for the pharmacist account.</param>
/// <param name="BranchId">Branch to assign the pharmacist to, or <see langword="null"/> for tenant-wide access.</param>
public sealed record InviteUserRequest(
    string Email,
    string FullName,
    string Password,
    Guid? BranchId);
