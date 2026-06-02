namespace Teryaq.Application.Features.Products.Dtos;

/// <summary>Payload for fully replacing an existing product.</summary>
/// <param name="Name">New display name.</param>
/// <param name="Description">New description, or <see langword="null"/> to clear it.</param>
/// <param name="Price">New unit price; must be greater than zero.</param>
public sealed record UpdateProductRequest(string Name, string? Description, decimal Price);
