namespace Teryaq.IntegrationTests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Teryaq.Application.Common.Tenancy;
using Teryaq.Infrastructure.Persistence;
using Teryaq.Infrastructure.Persistence.Interceptors;

/// <summary>Bootstraps the full application with an in-memory SQLite database for integration testing.</summary>
/// <remarks>
/// Keeps one <see cref="SqliteConnection"/> open for the lifetime of the factory so that the
/// in-memory database is not destroyed between requests. Each test resets the schema via
/// <c>EnsureDeleted</c> + <c>EnsureCreated</c> in its <c>InitializeAsync</c>.
/// </remarks>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Fixed tenant identifier used across all integration test requests.</summary>
    public static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _connection.Open();

        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["Jwt:Secret"] = "test-only-secret-at-least-32-characters!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            // Override ICurrentTenant with a fixed test tenant so query filters resolve correctly.
            services.RemoveAll<ICurrentTenant>();
            services.AddScoped<ICurrentTenant>(_ => new TestCurrentTenant(TestTenantId));

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<TenantInterceptor>());
            });
        });
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _connection.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>In-memory <see cref="ICurrentTenant"/> implementation for integration tests.</summary>
    private sealed class TestCurrentTenant : ICurrentTenant
    {
        private readonly Guid _tenantId;

        internal TestCurrentTenant(Guid tenantId) => _tenantId = tenantId;

        /// <inheritdoc/>
        public Guid TenantId => _tenantId;

        /// <inheritdoc/>
        public Guid? BranchId => null;
    }
}
