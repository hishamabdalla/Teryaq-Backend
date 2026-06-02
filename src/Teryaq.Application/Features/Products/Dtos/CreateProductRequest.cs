namespace Teryaq.Application.Features.Products.Dtos;

/// <summary>Payload for creating a new product.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Price">Unit price; must be greater than zero.</param>
public sealed record CreateProductRequest(string Name, string? Description, decimal Price);
