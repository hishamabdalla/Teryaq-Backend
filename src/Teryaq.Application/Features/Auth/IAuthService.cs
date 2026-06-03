namespace Teryaq.Application.Features.Auth;

using Teryaq.Application.Common;
using Teryaq.Application.Features.Auth.Dtos;

/// <summary>Handles tenant registration, user authentication, and token refresh.</summary>
public interface IAuthService
{
    /// <summary>Registers a new pharmacy: creates the tenant, main branch, and owner user in a single transaction.</summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Authenticates a user by email and password and returns a signed JWT with a refresh token.</summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Exchanges a valid refresh token for a new access token and rotated refresh token.</summary>
    /// <param name="request">Refresh token payload.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
}
