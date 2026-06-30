namespace Teryaq.Application.Features.Customers;

using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Features.Customers.Dtos;

/// <summary>Manages customer profiles for the current tenant.</summary>
public interface ICustomerService
{
    /// <summary>Returns a paginated list of customers, optionally filtered by name or phone.</summary>
    /// <param name="search">Optional substring matched against name or phone.</param>
    /// <param name="pagination">Page number and size.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<PaginatedList<CustomerDto>>> GetPagedAsync(string? search, PaginationParams pagination, CancellationToken ct = default);

    /// <summary>Returns a customer by identifier, or a not-found error if it does not exist.</summary>
    /// <param name="id">Customer identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Registers a new customer for the current tenant.</summary>
    /// <param name="request">Customer details.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
}
