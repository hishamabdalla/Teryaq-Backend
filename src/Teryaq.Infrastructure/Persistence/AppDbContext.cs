namespace Teryaq.Infrastructure.Persistence;

using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Teryaq.Application.Common.Tenancy;
using Teryaq.Domain.Common;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Drugs;
using Teryaq.Domain.Features.Tenants;
using Teryaq.Infrastructure.Identity;

/// <summary>The application's primary EF Core database context. Extends Identity and applies tenant + soft-delete global filters.</summary>
public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentTenant _currentTenant;

    /// <summary>Initialises a new instance of <see cref="AppDbContext"/>.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant) : base(options)
    {
        _currentTenant = currentTenant;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }

    /// <summary>Gets the tenants table.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Gets the branches table.</summary>
    public DbSet<Branch> Branches => Set<Branch>();

    /// <summary>Gets the drugs table.</summary>
    public DbSet<Drug> Drugs => Set<Drug>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        RenameIdentityTables(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ApplyTenantFilters(builder);
    }

    private static void RenameIdentityTables(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }

    private void ApplyTenantFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (typeof(ITenantEntity).IsAssignableFrom(clrType) && typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                typeof(AppDbContext)
                    .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clrType)
                    .Invoke(this, [builder]);
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : BaseEntity, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _currentTenant.TenantId);
    }
}
