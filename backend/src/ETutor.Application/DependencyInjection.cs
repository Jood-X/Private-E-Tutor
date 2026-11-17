using ETutor.Application.Catalog;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ETutor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<CatalogService>();
        return services;
    }
}
