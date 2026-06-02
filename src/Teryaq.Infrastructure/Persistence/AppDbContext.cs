namespace Teryaq.Infrastructure.Persistence;

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Teryaq.Domain.Features.Products;

/// <summary>The application's primary EF Core database context. Entity configurations are applied from the current assembly.</summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>Initialises a new instance of <see cref="AppDbContext"/>.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
    }

    /// <summary>Gets the products table.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
