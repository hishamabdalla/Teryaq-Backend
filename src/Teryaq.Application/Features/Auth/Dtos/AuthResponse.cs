namespace Teryaq.Application.Features.Auth.Dtos;

/// <summary>Returned on successful registration, login, or token refresh.</summary>
/// <param name="AccessToken">Signed JWT access token.</param>
/// <param name="RefreshToken">Opaque refresh token used to obtain a new access token.</param>
/// <param name="ExpiresAt">UTC timestamp when the access token expires.</param>
/// <param name="User">Summary of the authenticated user.</param>
/// <param name="TenantId">The authenticated user's tenant identifier (mirrors <see cref="AuthUserDto.TenantId"/> for convenience).</param>
/// <param name="BranchId">The authenticated user's branch identifier, or <see langword="null"/> for owner-level tokens.</param>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    AuthUserDto User,
    Guid TenantId,
    Guid? BranchId);
