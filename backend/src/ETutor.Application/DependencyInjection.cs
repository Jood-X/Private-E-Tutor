using ETutor.Application.Catalog;
using ETutor.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ETutor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<CatalogService>();

        services.AddScoped<StudyPlanRules>();
        services.AddScoped<ProfileQueries>();
        services.AddScoped<TokenIssuer>();
        services.AddScoped<RegistrationService>();
        services.AddScoped<SessionService>();
        services.AddScoped<ProfileService>();
        return services;
    }
}
