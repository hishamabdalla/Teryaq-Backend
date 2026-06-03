namespace Teryaq.Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Teryaq.Application.Common.Tenancy;
using Teryaq.Domain.Common;

/// <summary>Stamps <see cref="ITenantEntity.TenantId"/> on every newly inserted tenant-scoped entity.</summary>
public sealed class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenant _currentTenant;

    /// <summary>Initialises a new instance of <see cref="TenantInterceptor"/>.</summary>
    public TenantInterceptor(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        StampTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        StampTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampTenantId(DbContext? context)
    {
        if (context is null) return;

        var tenantId = _currentTenant.TenantId;
        if (tenantId == Guid.Empty) return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                entry.Entity.TenantId = tenantId;
        }
    }
}
