namespace Teryaq.Application.Features.Customers.Dtos;

/// <summary>Customer profile data returned by the API.</summary>
/// <param name="Id">Customer identifier.</param>
/// <param name="Name">Customer display name.</param>
/// <param name="Phone">Contact phone number, or <see langword="null"/> if not provided.</param>
/// <param name="Email">Email address, or <see langword="null"/> if not provided.</param>
public sealed record CustomerDto(Guid Id, string Name, string? Phone, string? Email);
