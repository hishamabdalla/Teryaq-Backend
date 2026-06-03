namespace Teryaq.Infrastructure.Services;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Teryaq.Application.Common.Settings;
using Teryaq.Application.Common.Tokens;

/// <summary>Creates signed JWT access tokens and random refresh tokens.</summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    /// <summary>Initialises a new instance of <see cref="JwtTokenGenerator"/>.</summary>
    public JwtTokenGenerator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <inheritdoc/>
    public string GenerateAccessToken(Guid userId, Guid tenantId, Guid? branchId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = userId.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            ["tenant_id"] = tenantId.ToString(),
            ["role"] = role,
        };

        if (branchId.HasValue)
            claims["branch_id"] = branchId.Value.ToString();

        var descriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            SigningCredentials = credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <inheritdoc/>
    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
