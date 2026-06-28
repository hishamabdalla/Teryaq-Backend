namespace Teryaq.Domain.Features.Alerts;

/// <summary>Identifies the kind of alert raised against a stock batch.</summary>
public enum AlertType
{
    /// <summary>The batch expires within the configured near-expiry window.</summary>
    NearExpiry,

    /// <summary>The on-hand quantity is at or below the batch's configured reorder level.</summary>
    LowStock,
}
