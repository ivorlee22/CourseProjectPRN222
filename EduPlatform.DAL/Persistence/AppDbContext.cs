using EduPlatform.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<RetrievalLog> RetrievalLogs => Set<RetrievalLog>();

    public DbSet<Package> Packages => Set<Package>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Payment> Payments => Set<Payment>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        ConfigureUsers(modelBuilder);
        ConfigureCourses(modelBuilder);
        ConfigureEnrollments(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureChat(modelBuilder);
        ConfigureCommerce(modelBuilder);
        SeedUsers(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.ToTable("Users");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
        entity.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
        entity.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
        entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(24);
        entity.HasIndex(x => x.NormalizedEmail).IsUnique();
    }

    private static void ConfigureCourses(ModelBuilder modelBuilder)
    {
        var course = modelBuilder.Entity<Course>();
        course.ToTable("Courses");
        course.HasKey(x => x.Id);
        course.Property(x => x.Title).HasMaxLength(200).IsRequired();
        course.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        course.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
        course.Property(x => x.EnrollmentPasswordHash).HasMaxLength(100);
        course.HasIndex(x => new { x.OwnerId, x.IsVisible });
        course.HasIndex(x => x.Title);
        course.HasOne(x => x.Owner)
            .WithMany(x => x.CreatedCourses)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEnrollments(ModelBuilder modelBuilder)
    {
        var enrollment = modelBuilder.Entity<CourseEnrollment>();
        enrollment.ToTable("CourseEnrollments");
        enrollment.HasKey(x => x.Id);
        enrollment.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        enrollment.HasIndex(x => new { x.CourseId, x.UserId }).IsUnique();
        enrollment.HasOne(x => x.Course)
            .WithMany(x => x.Enrollments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        enrollment.HasOne(x => x.User)
            .WithMany(x => x.CourseEnrollments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        var document = modelBuilder.Entity<Document>();
        document.ToTable("Documents");
        document.HasKey(x => x.Id);
        document.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        document.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        document.Property(x => x.ContentType).HasMaxLength(160).IsRequired();
        document.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        document.Property(x => x.FailureReason).HasMaxLength(2000);
        document.HasIndex(x => new { x.CourseId, x.Status });
        document.HasOne(x => x.Course)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        var chunk = modelBuilder.Entity<DocumentChunk>();
        chunk.ToTable("DocumentChunks");
        chunk.HasKey(x => x.Id);
        chunk.Property(x => x.Content).IsRequired();
        chunk.Property(x => x.Section).HasMaxLength(300);
        chunk.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        chunk.Property(x => x.Embedding).HasColumnType("vector(3072)");
        chunk.HasIndex(x => new { x.DocumentId, x.Sequence }).IsUnique();
        chunk.HasOne(x => x.Document)
            .WithMany(x => x.Chunks)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureChat(ModelBuilder modelBuilder)
    {
        var session = modelBuilder.Entity<ChatSession>();
        session.ToTable("ChatSessions");
        session.HasKey(x => x.Id);
        session.Property(x => x.Title).HasMaxLength(200).IsRequired();
        session.HasIndex(x => new { x.UserId, x.LastMessageAtUtc });
        session.HasOne(x => x.User)
            .WithMany(x => x.ChatSessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        session.HasOne(x => x.Course)
            .WithMany(x => x.ChatSessions)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        var message = modelBuilder.Entity<Message>();
        message.ToTable("Messages");
        message.HasKey(x => x.Id);
        message.Property(x => x.Role).HasConversion<string>().HasMaxLength(24);
        message.Property(x => x.Content).IsRequired();
        message.HasIndex(x => new { x.ChatSessionId, x.CreatedAtUtc });
        message.HasOne(x => x.ChatSession)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        var retrieval = modelBuilder.Entity<RetrievalLog>();
        retrieval.ToTable("RetrievalLogs");
        retrieval.HasKey(x => x.Id);
        retrieval.HasIndex(x => new { x.MessageId, x.DocumentChunkId }).IsUnique();
        retrieval.HasOne(x => x.Message)
            .WithMany(x => x.RetrievalLogs)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        retrieval.HasOne(x => x.DocumentChunk)
            .WithMany(x => x.RetrievalLogs)
            .HasForeignKey(x => x.DocumentChunkId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureCommerce(ModelBuilder modelBuilder)
    {
        var package = modelBuilder.Entity<Package>();
        package.ToTable("Packages");
        package.HasKey(x => x.Id);
        package.Property(x => x.Name).HasMaxLength(80).IsRequired();
        package.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        package.Property(x => x.Price).HasPrecision(18, 2);
        package.HasIndex(x => x.Name).IsUnique();

        var subscription = modelBuilder.Entity<Subscription>();
        subscription.ToTable("Subscriptions");
        subscription.HasKey(x => x.Id);
        subscription.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        subscription.HasIndex(x => new { x.UserId, x.Status });
        subscription.HasOne(x => x.User)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        subscription.HasOne(x => x.Package)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        var payment = modelBuilder.Entity<Payment>();
        payment.ToTable("Payments");
        payment.HasKey(x => x.Id);
        payment.Property(x => x.Amount).HasPrecision(18, 2);
        payment.Property(x => x.Method).HasConversion<string>().HasMaxLength(24);
        payment.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        payment.Property(x => x.InternalReference).HasMaxLength(100).IsRequired();
        payment.Property(x => x.GatewayTransactionId).HasMaxLength(160);
        payment.Property(x => x.GatewayResponseCode).HasMaxLength(80);
        payment.Property(x => x.RawResponseJson).HasColumnType("jsonb");
        payment.HasIndex(x => x.InternalReference).IsUnique();
        payment.HasIndex(x => new { x.Method, x.GatewayTransactionId })
            .IsUnique()
            .HasFilter("\"GatewayTransactionId\" IS NOT NULL");
        payment.HasOne(x => x.User)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        payment.HasOne(x => x.Package)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
        payment.HasOne(x => x.Subscription)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Email = "admin@eduplatform.local",
                NormalizedEmail = "ADMIN@EDUPLATFORM.LOCAL",
                PasswordHash = "$2b$12$rk9iUtt9Cv4x2vHR99pLC.JH4Z51DHCR/v/650iN79vM1f.OFnbCi",
                FullName = "System Administrator",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAtUtc = seededAt,
                UpdatedAtUtc = seededAt
            },
            new User
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Email = "teacher@eduplatform.local",
                NormalizedEmail = "TEACHER@EDUPLATFORM.LOCAL",
                PasswordHash = "$2b$12$AaGe.mAiXrDAQP5Oq3MdPe.670a9Ctzx7Go0Bsgz7lSQeySz6aj16",
                FullName = "Demo Teacher",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAtUtc = seededAt,
                UpdatedAtUtc = seededAt
            },
            new User
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Email = "student@eduplatform.local",
                NormalizedEmail = "STUDENT@EDUPLATFORM.LOCAL",
                PasswordHash = "$2b$12$g9IkcdaBqqZgjXQNXIboCuqpQ2miyjMXKkylbvTouFuoR0IrcOlUS",
                FullName = "Demo Student",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAtUtc = seededAt,
                UpdatedAtUtc = seededAt
            });
    }

    private void UpdateTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAtUtc).IsModified = false;
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
