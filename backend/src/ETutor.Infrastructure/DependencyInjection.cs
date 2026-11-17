using ETutor.Application.Abstractions;
using ETutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ETutor.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseConnectionStringName = "Database";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DatabaseConnectionStringName}' is not configured. " +
                "Set ConnectionStrings__Database (see docs/setup.md).");
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
