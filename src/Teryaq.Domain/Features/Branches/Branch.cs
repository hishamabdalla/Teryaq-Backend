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

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private Branch() { }

    /// <summary>Creates the primary branch for a newly registered tenant.</summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="name">Display name of the branch.</param>
    /// <param name="address">Optional street address.</param>
    /// <param name="phone">Optional contact phone number.</param>
    public static Branch CreateMain(Guid tenantId, string name, string? address = null, string? phone = null) =>
        new() { TenantId = tenantId, Name = name, Address = address, Phone = phone, IsMain = true };
}
