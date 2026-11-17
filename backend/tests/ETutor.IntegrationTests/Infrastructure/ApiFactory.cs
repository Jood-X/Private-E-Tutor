using ETutor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ETutor.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API against a real SQL Server database (LocalDB by default). The database is
/// dropped and rebuilt from the migrations once per test run, which also proves the migrations
/// are reproducible. Override the connection with the ETUTOR_TEST_DB environment variable.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestEnvironment = "Testing";

    private const string DefaultConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=ETutor_IntegrationTests;Trusted_Connection=True;TrustServerCertificate=True";

    protected virtual int RateLimitPermits => 10_000;

    public virtual async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);
        builder.UseSetting("ConnectionStrings:Database",
            Environment.GetEnvironmentVariable("ETUTOR_TEST_DB") ?? DefaultConnectionString);
        builder.UseSetting("RateLimiting:PermitLimit", RateLimitPermits.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("RateLimiting:WindowSeconds", "60");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(TestOnlyController).Assembly));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
