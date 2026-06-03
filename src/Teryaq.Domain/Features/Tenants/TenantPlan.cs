namespace Teryaq.Domain.Features.Tenants;

/// <summary>Subscription plan assigned to a <see cref="Tenant"/>.</summary>
public enum TenantPlan
{
    /// <summary>Single-branch pharmacy — $50/month.</summary>
    Solo = 0,

    /// <summary>Up to two branches — $100/month.</summary>
    TwoBranches = 1,

    /// <summary>Chain of up to ten branches — $200+/month.</summary>
    Chain = 2,
}
