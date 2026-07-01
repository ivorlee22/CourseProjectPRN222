using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using EduPlatform.BLL.Services;
using EduPlatform.DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduPlatform.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDataAccess(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICourseService, CourseService>();
        // TEMPORARY: Nguyên must replace this registration with the
        // subscription-backed quota service. See AGENTS.md handoff section.
        services.AddScoped<ICourseQuotaService, DeferredCourseQuotaService>();
        services.AddScoped<IEmailService, GmailEmailService>();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        return services;
    }
}
