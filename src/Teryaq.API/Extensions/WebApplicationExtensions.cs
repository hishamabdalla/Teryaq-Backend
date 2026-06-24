namespace Teryaq.API.Extensions;

using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Teryaq.Infrastructure.Persistence;
using Teryaq.Infrastructure.Persistence.Seed;

/// <summary>Extension methods for configuring the <see cref="WebApplication"/> middleware pipeline.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Runs EF Core migrations and seeds reference data in non-development environments.</summary>
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await db.Database.MigrateAsync();
        await DrugSeeder.SeedAsync(db, logger);
    }

    /// <summary>Configures the HTTP middleware pipeline and maps all endpoints.</summary>
    public static WebApplication UseAPI(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();
        app.UseCors();
        app.UseRateLimiter();
        app.UseResponseCompression();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<Teryaq.API.Middleware.PlanLimitMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Teryaq API v1");
            options.RoutePrefix = "swagger";
        });

        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Teryaq API")
                   .WithTheme(ScalarTheme.DeepSpace)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                   .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
        });

        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }
}
