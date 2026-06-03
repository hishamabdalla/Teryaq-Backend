namespace Teryaq.Application.Common.Tokens;

/// <summary>Generates signed JWT access tokens and opaque refresh tokens.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Creates a signed JWT carrying tenant, branch, and role claims.</summary>
    /// <param name="userId">The authenticated user's identifier.</param>
    /// <param name="tenantId">The user's tenant identifier.</param>
    /// <param name="branchId">The user's branch, or <see langword="null"/> for owner-level tokens.</param>
    /// <param name="role">The user's role name.</param>
    string GenerateAccessToken(Guid userId, Guid tenantId, Guid? branchId, string role);

    /// <summary>Creates a cryptographically random opaque refresh token.</summary>
    string GenerateRefreshToken();
}
