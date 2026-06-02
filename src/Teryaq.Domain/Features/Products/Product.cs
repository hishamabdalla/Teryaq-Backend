namespace Teryaq.Domain.Features.Products;

using Teryaq.Domain.Common;

/// <summary>Aggregate root representing a product in the catalogue.</summary>
public sealed class Product : BaseEntity
{
    private Product() { }

    /// <summary>Gets the display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the unit price.</summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Creates and returns a new <see cref="Product"/>.
    /// Raises <see cref="ProductCreatedEvent"/> as a side-effect.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="price">Unit price.</param>
    public static Product Create(string name, string? description, decimal price)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name));
        return product;
    }

    /// <summary>Replaces all mutable fields with new values.</summary>
    /// <param name="name">New display name.</param>
    /// <param name="description">New description, or <see langword="null"/> to clear it.</param>
    /// <param name="price">New unit price.</param>
    public void Update(string name, string? description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
    }
}
