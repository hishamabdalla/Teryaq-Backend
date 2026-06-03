namespace Teryaq.Application.Common.Settings;

/// <summary>JWT token configuration bound from the <c>Jwt</c> configuration section.</summary>
public sealed class JwtSettings
{
    /// <summary>Gets the token issuer.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Gets the token audience.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Gets the HMAC-SHA256 signing secret (minimum 32 characters).</summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>Gets the access token lifetime in minutes. Defaults to 60.</summary>
    public int ExpiryMinutes { get; init; } = 60;

    /// <summary>Gets the refresh token lifetime in days. Defaults to 7.</summary>
    public int RefreshExpiryDays { get; init; } = 7;
}
