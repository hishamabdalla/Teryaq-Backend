namespace Teryaq.Domain.Features.Tenants;

/// <summary>Tracks the onboarding progress of a newly registered pharmacy tenant.</summary>
public enum OnboardingStatus
{
    /// <summary>Tenant has registered but has not completed onboarding.</summary>
    Pending = 0,

    /// <summary>Tenant has completed all required onboarding steps.</summary>
    Completed = 1,
}
