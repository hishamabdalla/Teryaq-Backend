namespace Teryaq.Application.Common.Settings;

/// <summary>Alert configuration bound from the <c>Alerts</c> configuration section.</summary>
public sealed class AlertSettings
{
    /// <summary>Gets the number of days before expiry at which a batch is flagged as near-expiry. Defaults to 90.</summary>
    public int NearExpiryDays { get; init; } = 90;

    /// <summary>Gets the days-until-expiry threshold at or below which a near-expiry alert is rated <c>High</c>. Defaults to 30.</summary>
    public int NearExpiryHighDays { get; init; } = 30;

    /// <summary>Gets the days-until-expiry threshold at or below which a near-expiry alert is rated <c>Medium</c> (above <see cref="NearExpiryHighDays"/>). Defaults to 60.</summary>
    public int NearExpiryMediumDays { get; init; } = 60;

    /// <summary>Gets the on-hand / reorder-level ratio at or below which a low-stock alert is rated <c>High</c>. Defaults to 0.25.</summary>
    public double LowStockHighRatio { get; init; } = 0.25;

    /// <summary>Gets the on-hand / reorder-level ratio at or below which a low-stock alert is rated <c>Medium</c> (above <see cref="LowStockHighRatio"/>). Defaults to 0.50.</summary>
    public double LowStockMediumRatio { get; init; } = 0.50;
}
