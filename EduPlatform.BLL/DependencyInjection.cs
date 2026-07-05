using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using EduPlatform.BLL.Services;
using EduPlatform.BLL.Services.TextExtractors;
using EduPlatform.DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

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
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        // TEMPORARY: Nguyên must replace this registration with the
        // subscription-backed quota service. See AGENTS.md handoff section.
        services.AddScoped<ICourseQuotaService, DeferredCourseQuotaService>();
        services.AddScoped<IEmailService, GmailEmailService>();

        // Document pipeline
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddHttpClient<IFileStorageService, CloudflareR2FileStorageService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        services.AddSingleton<ITextChunker, FixedSizeTextChunker>();
        services.AddSingleton<ITextExtractor, PdfTextExtractor>();
        services.AddSingleton<ITextExtractor, DocxTextExtractor>();
        services.AddSingleton<ITextExtractor>(_ =>
            new PlainTextExtractor(DocumentFileType.Txt));
        services.AddSingleton<ITextExtractor>(_ =>
            new PlainTextExtractor(DocumentFileType.Md));

        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));
        services.AddOptions<DocumentOptions>()
            .Bind(configuration.GetSection(DocumentOptions.SectionName));

        return services;
    }
}
