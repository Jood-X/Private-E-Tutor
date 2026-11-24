using ETutor.Application.Abstractions;
using ETutor.Infrastructure.Files;
using ETutor.Infrastructure.Identity;
using ETutor.Infrastructure.Persistence;
using ETutor.Infrastructure.Persistence.Seed;
using ETutor.Infrastructure.Videos;
using Microsoft.AspNetCore.Identity;
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

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .AddInterceptors(provider.GetRequiredService<AuditingInterceptor>()));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<IDatabaseErrors, SqlServerDatabaseErrors>();
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<YouTubeOptions>(configuration.GetSection(YouTubeOptions.SectionName));
        services.AddHttpClient<IVideoProvider, YouTubeVideoProvider>();

        services.AddIdentityInfrastructure(configuration);
        services.AddScoped<DevDataSeeder>();
        return services;
    }

    private static void AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 4;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<ITokenService, TokenService>();
    }
}
