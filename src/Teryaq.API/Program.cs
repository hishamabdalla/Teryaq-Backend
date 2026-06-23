using Serilog;
using Teryaq.API.Extensions;
using Teryaq.Application;
using Teryaq.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddAPI(builder.Configuration);

    var app = builder.Build();

    await app.InitialiseDatabaseAsync();

    app.UseAPI();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposes the entry-point type for <c>WebApplicationFactory</c> in integration tests.</summary>
public partial class Program { }
