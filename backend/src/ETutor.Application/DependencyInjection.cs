using ETutor.Application.Catalog;
using ETutor.Application.Courses;
using ETutor.Application.Discovery;
using ETutor.Application.Enrollments;
using ETutor.Application.Learning;
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

        services.AddScoped<TeacherCourseService>();
        services.AddScoped<CourseContentService>();
        services.AddScoped<LessonQueries>();
        services.AddScoped<Videos.VideoResourceService>();
        services.AddScoped<LessonResourceService>();
        services.AddScoped<EnrollmentService>();
        services.AddScoped<TeacherEnrollmentService>();
        services.AddScoped<DiscoveryService>();
        services.AddScoped<LearningService>();
        return services;
    }
}
