namespace Teryaq.Application.Features.Customers.Dtos;

/// <summary>Request payload to register a new customer.</summary>
/// <param name="Name">Customer display name. Required, maximum 200 characters.</param>
/// <param name="Phone">Contact phone number. Optional, maximum 20 characters.</param>
/// <param name="Email">Email address. Optional, must be a valid email format when provided.</param>
public sealed record CreateCustomerRequest(string Name, string? Phone, string? Email);
