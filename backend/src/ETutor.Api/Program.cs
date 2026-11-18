using ETutor.Api.Configuration;
using ETutor.Application;
using ETutor.Infrastructure;
using Serilog;

// Startup logger for failures before configuration is loaded. Deliberately not a "bootstrap"
// (reloadable) logger: that one can be frozen only once per process, which breaks test hosts.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices(builder.Configuration);

    var app = builder.Build();

    if (args.Contains(DevSeedCommand.Name, StringComparer.Ordinal))
    {
        await DevSeedCommand.RunAsync(app);
        return;
    }

    app.UseApiPipeline();
    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point; public so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
