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
        services.AddHttpClient();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ICourseQuotaService, SubscriptionCourseQuotaService>();

        services.Configure<EduPlatform.BLL.Options.VNPayOptions>(configuration.GetSection(EduPlatform.BLL.Options.VNPayOptions.SectionName));
        services.Configure<EduPlatform.BLL.Options.MoMoOptions>(configuration.GetSection(EduPlatform.BLL.Options.MoMoOptions.SectionName));

        services.AddScoped<IVNPayService, VNPayService>();
        services.AddScoped<IMoMoService, MoMoService>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<IChatQuotaService, SubscriptionChatQuotaService>();
        services.AddScoped<IEmailService, GmailEmailService>();
        services.AddScoped<IChatService, ChatService>();

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
        services.AddHttpClient<IChatCompletionService, GeminiChatCompletionService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));
        services.AddOptions<DocumentOptions>()
            .Bind(configuration.GetSection(DocumentOptions.SectionName));
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName));
        services.AddOptions<ChatOptions>()
            .Bind(configuration.GetSection(ChatOptions.SectionName));

        return services;
    }
}
