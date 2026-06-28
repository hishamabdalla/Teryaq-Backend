namespace Teryaq.Domain.Features.Alerts;

/// <summary>Indicates the urgency level of an alert raised against a stock batch.</summary>
public enum AlertSeverity
{
    /// <summary>Requires immediate attention — expiry is within 30 days, or on-hand quantity is at most 25 % of the reorder level.</summary>
    High,

    /// <summary>Warrants attention soon — expiry is within 31–60 days, or on-hand quantity is 26–50 % of the reorder level.</summary>
    Medium,

    /// <summary>Early warning — expiry is within 61–90 days, or on-hand quantity is 51–100 % of the reorder level.</summary>
    Low,
}
