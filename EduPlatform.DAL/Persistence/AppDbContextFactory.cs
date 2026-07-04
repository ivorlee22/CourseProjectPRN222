using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace EduPlatform.DAL.Persistence;

/// <summary>
/// Provides design-time connections for the EF Core tools. Resolution order:
/// 1. <c>ConnectionStrings__MigrationConnection</c> (production Neon direct endpoint)
/// 2. <c>ConnectionStrings__DefaultConnection</c> (Neon pooled or local compose)
/// 3. Local docker-compose fallback (port 5466, see docker-compose.yml)
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__MigrationConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5466;Database=eduplatform;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .Options;

        return new AppDbContext(options);
    }
}