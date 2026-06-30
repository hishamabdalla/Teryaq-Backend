namespace Teryaq.Domain.Features.Customers;

using Teryaq.Domain.Interfaces;

/// <summary>Repository contract for <see cref="Customer"/> aggregates.</summary>
public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>Returns a filtered, paginated slice of customers ordered by name ascending.</summary>
    /// <param name="search">Optional substring matched against <see cref="Customer.Name"/> or <see cref="Customer.Phone"/>.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken ct = default);
}
