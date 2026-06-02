namespace Teryaq.API.Options;

using System.ComponentModel.DataAnnotations;

/// <summary>JWT authentication configuration bound from the <c>Jwt</c> section of <c>appsettings.json</c>.</summary>
public sealed class JwtOptions
{
    /// <summary>Gets the token issuer.</summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Gets the token audience.</summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Gets the HMAC-SHA256 signing secret. Must be at least 32 characters.</summary>
    [Required, MinLength(32)]
    public string Secret { get; init; } = string.Empty;
}
