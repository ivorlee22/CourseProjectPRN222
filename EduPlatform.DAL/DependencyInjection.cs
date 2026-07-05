using EduPlatform.DAL.Persistence;
using EduPlatform.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace EduPlatform.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required before database access.");
            }

            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector());
        });

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        return services;
    }
}
