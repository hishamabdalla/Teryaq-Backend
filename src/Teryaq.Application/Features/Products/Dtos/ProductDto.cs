namespace Teryaq.Application.Features.Products.Dtos;

/// <summary>Read model returned by product endpoints.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Price">Unit price.</param>
/// <param name="CreatedAt">UTC timestamp of creation.</param>
/// <param name="UpdatedAt">UTC timestamp of last update, or <see langword="null"/> if never updated.</param>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
