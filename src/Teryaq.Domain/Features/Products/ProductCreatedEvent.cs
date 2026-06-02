namespace Teryaq.Domain.Features.Products;

using Teryaq.Domain.Events;

/// <summary>Raised when a new product is successfully created.</summary>
/// <param name="ProductId">Identifier of the newly created product.</param>
/// <param name="Name">Display name of the newly created product.</param>
public sealed class ProductCreatedEvent(Guid ProductId, string Name) : DomainEvent
{
    /// <summary>Gets the identifier of the newly created product.</summary>
    public Guid ProductId { get; } = ProductId;

    /// <summary>Gets the display name of the newly created product.</summary>
    public string Name { get; } = Name;
}
