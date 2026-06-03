namespace Teryaq.Application.Features.Auth.Dtos;

/// <summary>Payload for exchanging a refresh token for a new access token.</summary>
/// <param name="RefreshToken">The opaque refresh token issued during login or a prior refresh.</param>
public sealed record RefreshTokenRequest(string RefreshToken);
