namespace Teryaq.API.Options;

/// <summary>Fixed-window rate-limiting configuration bound from the <c>RateLimit</c> section of <c>appsettings.json</c>.</summary>
public sealed class RateLimitOptions
{
    /// <summary>Gets the length of the rate-limit window in seconds. Defaults to <c>60</c>.</summary>
    public int WindowSeconds { get; init; } = 60;

    /// <summary>Gets the maximum number of requests allowed per window. Defaults to <c>100</c>.</summary>
    public int PermitLimit { get; init; } = 100;
}
