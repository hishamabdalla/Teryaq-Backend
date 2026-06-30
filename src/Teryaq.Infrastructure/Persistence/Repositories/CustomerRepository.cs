namespace Teryaq.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Teryaq.Domain.Features.Customers;

/// <summary>EF Core implementation of <see cref="ICustomerRepository"/>.</summary>
public sealed class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    /// <summary>Initialises a new instance of <see cref="CustomerRepository"/>.</summary>
    public CustomerRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = Context.Set<Customer>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Name, pattern) ||
                (c.Phone != null && EF.Functions.Like(c.Phone, pattern)));
        }

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
