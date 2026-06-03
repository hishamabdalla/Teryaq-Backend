namespace Teryaq.Domain.Features.Tenants;

/// <summary>Lifecycle status of a <see cref="Tenant"/> account.</summary>
public enum TenantStatus
{
    /// <summary>Free 90-day trial period.</summary>
    Trial = 0,

    /// <summary>Paid and fully active.</summary>
    Active = 1,

    /// <summary>Account suspended (e.g. non-payment).</summary>
    Suspended = 2,
}
