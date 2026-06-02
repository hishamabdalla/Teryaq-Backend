namespace Teryaq.Infrastructure;

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teryaq.Application.Common.Events;
using Teryaq.Domain.Interfaces;
using Teryaq.Infrastructure.Persistence;
using Teryaq.Infrastructure.Persistence.Interceptors;
using Teryaq.Infrastructure.Persistence.Repositories;
using Teryaq.Infrastructure.Services;

/// <summary>Registers all Infrastructure-layer services into the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds EF Core, interceptors, repositories, the unit of work, and the domain event dispatcher.</summary>
    /// <remarks>
    /// Set <c>Database:Provider</c> to <c>Sqlite</c> in configuration to use SQLite (e.g. for integration tests).
    /// Defaults to SQL Server. Any other value throws at startup.
    /// </remarks>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddPersistence(configuration);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        services.Scan(scan => scan
            .FromAssemblies(Assembly.GetExecutingAssembly())
            .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string? provider = configuration["Database:Provider"];

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "DataSource=:memory:");
            }
            else if (string.IsNullOrEmpty(provider) || string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }
            else
            {
                throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: SqlServer, Sqlite.");
            }

            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        return services;
    }
}
