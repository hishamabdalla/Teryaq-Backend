namespace Teryaq.IntegrationTests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Teryaq.Infrastructure.Persistence;

/// <summary>Bootstraps the full application with an in-memory SQLite database for integration testing.</summary>
/// <remarks>
/// Keeps one <see cref="SqliteConnection"/> open for the lifetime of the factory so that the
/// in-memory database is not destroyed between requests. Each test resets the schema via
/// <c>EnsureDeleted</c> + <c>EnsureCreated</c> in its <c>InitializeAsync</c>.
/// </remarks>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
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
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext-related registrations. We must also remove
            // IDbContextOptionsConfiguration<AppDbContext> because it holds the SQL Server
            // provider configuration — leaving it causes a "two providers registered" error
            // when the new SQLite registration is added on top.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(
                    sp.GetRequiredService<Teryaq.Infrastructure.Persistence.Interceptors.AuditInterceptor>(),
                    sp.GetRequiredService<Teryaq.Infrastructure.Persistence.Interceptors.SoftDeleteInterceptor>());
            });
        });
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}
