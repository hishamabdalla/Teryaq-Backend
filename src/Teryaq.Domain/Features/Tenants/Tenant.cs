namespace Teryaq.Domain.Features.Tenants;

using Teryaq.Domain.Common;

/// <summary>Represents a pharmacy business account (the top-level tenant). Not itself tenant-scoped — it is the root of tenancy.</summary>
public sealed class Tenant : BaseEntity
{
    /// <summary>Gets the display name of the pharmacy business.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the current subscription plan.</summary>
    public TenantPlan Plan { get; private set; }

    /// <summary>Gets the account lifecycle status.</summary>
    public TenantStatus Status { get; private set; }

    /// <summary>Gets the onboarding progress of this tenant.</summary>
    public OnboardingStatus OnboardingStatus { get; private set; }

    /// <summary>Gets the primary contact email for this pharmacy business.</summary>
    public string? ContactEmail { get; private set; }

    /// <summary>Gets the primary contact phone number for this pharmacy business.</summary>
    public string? ContactPhone { get; private set; }

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private Tenant() { }

    /// <summary>Creates a new <see cref="Tenant"/> in the trial status with the solo plan.</summary>
    /// <param name="name">Display name of the pharmacy business.</param>
    public static Tenant Create(string name) =>
        new() { Name = name, Plan = TenantPlan.Solo, Status = TenantStatus.Trial, OnboardingStatus = OnboardingStatus.Pending };

    /// <summary>Updates the mutable contact details of this tenant.</summary>
    /// <param name="contactEmail">New contact email address, or <see langword="null"/> to clear.</param>
    /// <param name="contactPhone">New contact phone number, or <see langword="null"/> to clear.</param>
    public void UpdateContact(string? contactEmail, string? contactPhone)
    {
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
    }

    /// <summary>Marks the tenant's onboarding as completed.</summary>
    public void CompleteOnboarding() => OnboardingStatus = OnboardingStatus.Completed;
}
