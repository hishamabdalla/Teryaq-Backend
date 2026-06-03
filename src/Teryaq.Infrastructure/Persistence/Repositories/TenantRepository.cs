namespace Teryaq.Infrastructure.Persistence.Repositories;

using Teryaq.Domain.Features.Tenants;

/// <summary>EF Core implementation of <see cref="ITenantRepository"/>.</summary>
public sealed class TenantRepository : GenericRepository<Tenant>, ITenantRepository
{
    /// <summary>Initialises a new instance of <see cref="TenantRepository"/>.</summary>
    public TenantRepository(AppDbContext context) : base(context) { }
}
