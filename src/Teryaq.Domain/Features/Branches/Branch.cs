namespace Teryaq.Domain.Features.Branches;

using Teryaq.Domain.Common;

/// <summary>Represents a physical branch of a pharmacy tenant.</summary>
public sealed class Branch : BaseEntity, ITenantEntity
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Gets the display name of this branch.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the street address of this branch.</summary>
    public string? Address { get; private set; }

    /// <summary>Gets the contact phone number of this branch.</summary>
    public string? Phone { get; private set; }

    /// <summary>Gets a value indicating whether this is the tenant's primary branch.</summary>
    public bool IsMain { get; private set; }

    /// <summary>Gets a value indicating whether this branch is currently active and operational.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private Branch() { }

    /// <summary>Creates the primary branch for a newly registered tenant.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="name">Display name of the branch.</param>
    /// <param name="address">Optional street address.</param>
    /// <param name="phone">Optional contact phone number.</param>
    public static Branch CreateMain(Guid tenantId, string name, string? address = null, string? phone = null) =>
        new() { TenantId = tenantId, Name = name, Address = address, Phone = phone, IsMain = true, IsActive = true };

    /// <summary>Creates a non-primary branch for an existing tenant.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="name">Display name of the branch.</param>
    /// <param name="address">Optional street address.</param>
    /// <param name="phone">Optional contact phone number.</param>
    public static Branch Create(Guid tenantId, string name, string? address = null, string? phone = null) =>
        new() { TenantId = tenantId, Name = name, Address = address, Phone = phone, IsMain = false, IsActive = true };

    /// <summary>Updates the mutable display information for this branch.</summary>
    /// <param name="name">New display name.</param>
    /// <param name="address">New street address, or <see langword="null"/> to clear.</param>
    /// <param name="phone">New contact phone number, or <see langword="null"/> to clear.</param>
    public void Update(string name, string? address, string? phone)
    {
        Name = name;
        Address = address;
        Phone = phone;
    }

    /// <summary>Deactivates this branch, preventing it from being used for new operations.</summary>
    public void Deactivate() => IsActive = false;
}
