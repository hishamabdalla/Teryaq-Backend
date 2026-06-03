namespace Teryaq.Application.Features.Auth.Dtos;

/// <summary>Payload for authenticating an existing user.</summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
public sealed record LoginRequest(string Email, string Password);
