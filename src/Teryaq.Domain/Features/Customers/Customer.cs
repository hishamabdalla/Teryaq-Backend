namespace Teryaq.Domain.Features.Customers;

using Teryaq.Domain.Common;

/// <summary>Represents a walk-in or registered customer of the pharmacy.</summary>
public sealed class Customer : BaseEntity, ITenantEntity
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Gets the customer's display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the customer's contact phone number, or <see langword="null"/> if not provided.</summary>
    public string? Phone { get; private set; }

    /// <summary>Gets the customer's email address, or <see langword="null"/> if not provided.</summary>
    public string? Email { get; private set; }

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private Customer() { }

    /// <summary>Creates a new <see cref="Customer"/>.</summary>
    /// <param name="name">Customer display name.</param>
    /// <param name="phone">Optional contact phone number.</param>
    /// <param name="email">Optional email address.</param>
    public static Customer Create(string name, string? phone, string? email) =>
        new()
        {
            Name = name,
            Phone = phone,
            Email = email,
        };
}
