using EduPlatform.BLL.DTOs.Courses;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Models;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Repositories;
using BllCourseType = EduPlatform.BLL.Enums.CourseType;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;
using DalCourseType = EduPlatform.DAL.Entities.CourseType;
using DalEnrollmentStatus = EduPlatform.DAL.Entities.EnrollmentStatus;
using DalUserRole = EduPlatform.DAL.Entities.UserRole;

namespace EduPlatform.Tests.Courses;

[TestClass]
public sealed class CourseServiceTests
{
    private static readonly Guid OwnerId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid StudentId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    private readonly FakeCourseRepository _repository = new();
    private readonly FakeCourseQuotaService _quotaService = new();
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        _service = new CourseService(
            _repository,
            _quotaService,
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 2, 8, 0, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public async Task CreateAsync_PublicCourse_PersistsWithCorrectOwner()
    {
        var adminActor = new ActorContext(Guid.NewGuid(), BllUserRole.Admin);
        var command = new CreateCourseCommand(
            "C# căn bản",
            "Nội dung lập trình C# cho người mới.",
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            OwnerId);

        var id = await _service.CreateAsync(command, adminActor, CancellationToken.None);

        var course = Assert.ContainsSingle(_repository.Courses);
        Assert.AreEqual(id, course.Id);
        Assert.AreEqual(OwnerId, course.OwnerId);
        Assert.AreEqual(DalCourseType.Public, course.Type);
        Assert.IsNull(course.EnrollmentPasswordHash);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task CreateAsync_WithoutTeacher_UsesAdminAsTemporaryOwner()
    {
        var adminActor = new ActorContext(Guid.Parse("20000000-0000-0000-0000-000000000099"), BllUserRole.Admin);
        var command = new CreateCourseCommand(
            "C# basics",
            "Introductory C# course content.",
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            OwnerId: null);

        await _service.CreateAsync(command, adminActor, CancellationToken.None);

        var course = Assert.ContainsSingle(_repository.Courses);
        Assert.AreEqual(adminActor.UserId, course.OwnerId);
    }

    [TestMethod]
    public async Task CreateAsync_PrivateCourse_HashesEnrollmentPassword()
    {
        var adminActor = new ActorContext(Guid.NewGuid(), BllUserRole.Admin);
        var command = new CreateCourseCommand(
            "Lớp nội bộ",
            "Khóa học chỉ dành cho thành viên được cấp mật khẩu.",
            BllCourseType.Private,
            IsVisible: true,
            EnrollmentPassword: "Course@123",
            OwnerId);

        await _service.CreateAsync(command, adminActor, CancellationToken.None);

        var course = Assert.ContainsSingle(_repository.Courses);
        Assert.IsNotNull(course.EnrollmentPasswordHash);
        Assert.AreNotEqual("Course@123", course.EnrollmentPasswordHash);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify(
            "Course@123",
            course.EnrollmentPasswordHash));
    }

    [TestMethod]
    public async Task CreateAsync_PrivateCourseWithoutPassword_ThrowsValidation()
    {
        var adminActor = new ActorContext(Guid.NewGuid(), BllUserRole.Admin);
        var command = new CreateCourseCommand(
            "Lớp nội bộ",
            "Khóa học chỉ dành cho thành viên được cấp mật khẩu.",
            BllCourseType.Private,
            IsVisible: true,
            EnrollmentPassword: null,
            OwnerId);

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(
            async () => await _service.CreateAsync(
                command,
                adminActor,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task CreateAsync_Teacher_ThrowsForbidden()
    {
        var teacherActor = new ActorContext(OwnerId, BllUserRole.Teacher);
        var command = new CreateCourseCommand(
            "C# căn bản",
            "Nội dung lập trình C# cho người mới.",
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            OwnerId);

        var exception = await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.CreateAsync(
                command,
                teacherActor,
                CancellationToken.None));
        
        Assert.IsFalse(string.IsNullOrWhiteSpace(exception.Message));
        Assert.IsEmpty(_repository.Courses);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task CreateAsync_AdminBypassesQuotaAndPersistsCourse()
    {
        var adminActor = new ActorContext(Guid.NewGuid(), BllUserRole.Admin);
        var command = new CreateCourseCommand(
            "C# basics",
            "Introductory C# course content.",
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            OwnerId);

        await _service.CreateAsync(command, adminActor, CancellationToken.None);

        Assert.HasCount(1, _repository.Courses);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task CreateAsync_Student_ThrowsForbidden()
    {
        var studentActor = new ActorContext(StudentId, BllUserRole.Student);
        var command = new CreateCourseCommand(
            "C# basics",
            "Introductory C# course content.",
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            OwnerId);

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.CreateAsync(
                command,
                studentActor,
                CancellationToken.None));

        Assert.IsEmpty(_repository.Courses);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task CreateAsync_TeacherCannotCreateForAnotherOwner()
    {
        var teacherActor = new ActorContext(OwnerId, BllUserRole.Teacher);
        var command = new CreateCourseCommand(
            "C# basics",
            "Introductory C# course content.",
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            Guid.NewGuid());

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.CreateAsync(
                command,
                teacherActor,
                CancellationToken.None));

        Assert.IsEmpty(_repository.Courses);
    }

    [TestMethod]
    public async Task UpdateAsync_NonOwner_ThrowsForbidden()
    {
        var course = CreateCourse();
        _repository.Courses.Add(course);
        var actor = new ActorContext(StudentId, BllUserRole.Student);
        var command = new UpdateCourseCommand(
            course.Title,
            course.Description,
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            RemoveEnrollmentPassword: false);

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.UpdateAsync(
                course.Id,
                command,
                actor,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task EnrollAsync_PrivateCourseWithWrongPassword_ThrowsValidation()
    {
        var course = CreateCourse(DalCourseType.Private);
        course.EnrollmentPasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct@123");
        _repository.Courses.Add(course);
        var actor = new ActorContext(StudentId, BllUserRole.Student);

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(
            async () => await _service.EnrollAsync(
                course.Id,
                "Wrong@123",
                actor,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task EnrollAsync_PublicCourse_AddsActiveEnrollment()
    {
        var course = CreateCourse();
        _repository.Courses.Add(course);
        var actor = new ActorContext(StudentId, BllUserRole.Student);

        await _service.EnrollAsync(course.Id, null, actor, CancellationToken.None);

        var enrollment = Assert.ContainsSingle(_repository.Enrollments);
        Assert.AreEqual(course.Id, enrollment.CourseId);
        Assert.AreEqual(StudentId, enrollment.UserId);
        Assert.AreEqual(DalEnrollmentStatus.Active, enrollment.Status);
        Assert.AreEqual(
            new DateTimeOffset(2026, 7, 2, 8, 0, 0, TimeSpan.Zero),
            enrollment.EnrolledAtUtc);
    }

    [TestMethod]
    public async Task InviteAsync_EmailLookup_AddsPendingEnrollment()
    {
        var course = CreateCourse();
        _repository.Courses.Add(course);
        _repository.Users.Add(new User
        {
            Id = StudentId,
            FullName = "Demo Student",
            Email = "student@example.test",
            NormalizedEmail = "STUDENT@EXAMPLE.TEST",
            Role = DalUserRole.Student,
            IsActive = true
        });

        await _service.InviteAsync(
            course.Id,
            "student@example.test",
            new ActorContext(OwnerId, BllUserRole.Teacher),
            CancellationToken.None);

        var enrollment = Assert.ContainsSingle(_repository.Enrollments);
        Assert.AreEqual(StudentId, enrollment.UserId);
        Assert.AreEqual(DalEnrollmentStatus.Pending, enrollment.Status);
    }

    [TestMethod]
    public async Task InviteAsync_DuplicateName_ThrowsValidation()
    {
        var course = CreateCourse();
        _repository.Courses.Add(course);
        _repository.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Demo Student",
            Email = "one@example.test",
            NormalizedEmail = "ONE@EXAMPLE.TEST",
            Role = DalUserRole.Student,
            IsActive = true
        });
        _repository.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Demo Student",
            Email = "two@example.test",
            NormalizedEmail = "TWO@EXAMPLE.TEST",
            Role = DalUserRole.Student,
            IsActive = true
        });

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(
            async () => await _service.InviteAsync(
                course.Id,
                "Demo Student",
                new ActorContext(OwnerId, BllUserRole.Teacher),
                CancellationToken.None));

        Assert.IsEmpty(_repository.Enrollments);
    }
    [TestMethod]
    public async Task CancelInvitationAsync_ValidInvitation_RemovesEnrollment()
    {
        var course = CreateCourse();
        _repository.Courses.Add(course);
        _repository.Enrollments.Add(new CourseEnrollment
        {
            CourseId = course.Id,
            UserId = StudentId,
            Status = DalEnrollmentStatus.Pending,
            InvitedById = OwnerId
        });

        await _service.CancelInvitationAsync(
            course.Id,
            StudentId,
            new ActorContext(OwnerId, BllUserRole.Teacher),
            CancellationToken.None);

        Assert.IsEmpty(_repository.Enrollments);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task CancelInvitationAsync_NonPendingInvitation_ThrowsNotFound()
    {
        var course = CreateCourse();
        _repository.Courses.Add(course);
        _repository.Enrollments.Add(new CourseEnrollment
        {
            CourseId = course.Id,
            UserId = StudentId,
            Status = DalEnrollmentStatus.Active
        });

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            async () => await _service.CancelInvitationAsync(
                course.Id,
                StudentId,
                new ActorContext(OwnerId, BllUserRole.Teacher),
                CancellationToken.None));
    }

    [TestMethod]
    public async Task CancelInvitationAsync_NotAuthorized_ThrowsForbidden()
    {
        var course = CreateCourse();
        _repository.Courses.Add(course);
        _repository.Enrollments.Add(new CourseEnrollment
        {
            CourseId = course.Id,
            UserId = StudentId,
            Status = DalEnrollmentStatus.Pending,
            InvitedById = OwnerId
        });

        var anotherTeacher = Guid.NewGuid();

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.CancelInvitationAsync(
                course.Id,
                StudentId,
                new ActorContext(anotherTeacher, BllUserRole.Teacher),
                CancellationToken.None));
    }

    [TestMethod]
    public async Task CreateAsync_CallsQuotaService_ToVerifyIntegration()
    {
        var adminActor = new ActorContext(Guid.NewGuid(), BllUserRole.Admin);
        var command = new CreateCourseCommand(
            "C# nâng cao",
            "Nội dung lập trình C#.",
            BllCourseType.Public,
            IsVisible: true,
            EnrollmentPassword: null,
            OwnerId);

        _repository.Courses.Add(new Course { OwnerId = OwnerId, Title = "C# 1", Description = "Desc", Type = CourseType.Public, IsVisible = true });

        var id = await _service.CreateAsync(command, adminActor, CancellationToken.None);

        Assert.AreEqual(1, _quotaService.EnsureCanCreateCourseCallCount);
        Assert.AreEqual(OwnerId, _quotaService.LastUserId);
        Assert.AreEqual(1, _quotaService.LastCurrentCourseCount);
    }

    [TestMethod]
    public async Task SearchAsync_AnonymousUser_RequestsVisibleCoursesOnly()
    {
        _repository.Courses.Add(CreateCourse());

        var result = await _service.SearchAsync(
            new CourseSearchQuery("C#"),
            actor: null,
            CancellationToken.None);

        Assert.HasCount(1, result.Items);
        Assert.IsTrue(_repository.LastVisibleOnly);
    }

    private static Course CreateCourse(DalCourseType type = DalCourseType.Public)
    {
        return new Course
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            OwnerId = OwnerId,
            Owner = new User
            {
                Id = OwnerId,
                FullName = "Course Owner",
                Email = "owner@example.test"
            },
            Title = "C# căn bản",
            Description = "Nội dung lập trình C# cho người mới.",
            Type = type,
            IsVisible = true
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class FakeCourseRepository : ICourseRepository
    {
        public List<Course> Courses { get; } = [];

        public List<CourseEnrollment> Enrollments { get; } = [];

        public List<User> Users { get; } = [];

        public int SaveChangesCallCount { get; private set; }

        public bool LastVisibleOnly { get; private set; }

        public Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Courses.SingleOrDefault(x => x.Id == id));
        }

        public Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
            string? keyword,
            int pageNumber,
            int pageSize,
            bool visibleOnly,
            Guid? ownerId,
            Guid? enrolledUserId,
            CancellationToken cancellationToken)
        {
            LastVisibleOnly = visibleOnly;
            var items = Courses
                .Where(x => !visibleOnly || x.IsVisible)
                .Where(x => !ownerId.HasValue || x.OwnerId == ownerId.Value)
                .Where(x => !enrolledUserId.HasValue || x.Enrollments.Any(
                    e => e.UserId == enrolledUserId.Value && e.Status == DalEnrollmentStatus.Active))
                .ToArray();
            return Task.FromResult<(IReadOnlyList<Course>, int)>((items, items.Length));
        }

        public Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Courses.Count(x => x.OwnerId == ownerId));
        }

        public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<User>> FindActiveStudentsByEmailOrNameAsync(
            string lookup,
            CancellationToken cancellationToken)
        {
            var normalizedLookup = lookup.Trim().ToUpperInvariant();
            var nameLookup = lookup.Trim();
            var users = Users
                .Where(x =>
                    x.IsActive
                    && x.Role == DalUserRole.Student
                    && (x.NormalizedEmail == normalizedLookup || x.FullName == nameLookup))
                .ToArray();
            return Task.FromResult<IReadOnlyList<User>>(users);
        }

        public Task<CourseEnrollment?> GetEnrollmentAsync(
            Guid courseId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Enrollments.SingleOrDefault(
                x => x.CourseId == courseId && x.UserId == userId));
        }

        public Task<IReadOnlyList<PendingCourseInvitation>> GetPendingInvitationsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var items = Enrollments
                .Where(x =>
                    x.UserId == userId
                    && x.Status == DalEnrollmentStatus.Pending
                    && x.InvitedById != null)
                .Select(x => new PendingCourseInvitation(
                    x.CourseId,
                    Courses.FirstOrDefault(c => c.Id == x.CourseId)?.Title ?? string.Empty,
                    "Quan tri vien"))
                .ToArray();
            return Task.FromResult<IReadOnlyList<PendingCourseInvitation>>(items);
        }

        public Task<int> CountPendingInvitationsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Enrollments.Count(x =>
                x.UserId == userId
                && x.Status == DalEnrollmentStatus.Pending
                && x.InvitedById != null));
        }

        public Task<IReadOnlyList<CourseEnrollment>> GetStudentsAsync(
            Guid courseId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CourseEnrollment>>(
                Enrollments.Where(x => x.CourseId == courseId).ToArray());
        }

        public Task AddAsync(Course course, CancellationToken cancellationToken)
        {
            Courses.Add(course);
            return Task.CompletedTask;
        }

        public Task AddEnrollmentAsync(
            CourseEnrollment enrollment,
            CancellationToken cancellationToken)
        {
            Enrollments.Add(enrollment);
            return Task.CompletedTask;
        }

        public void Remove(Course course)
        {
            Courses.Remove(course);
        }

        public void RemoveEnrollment(CourseEnrollment enrollment)
        {
            Enrollments.Remove(enrollment);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCourseQuotaService : ICourseQuotaService
    {
        public int EnsureCanCreateCourseCallCount { get; private set; }
        public Guid LastUserId { get; private set; }
        public int LastCurrentCourseCount { get; private set; }

        public Task EnsureCanCreateCourseAsync(Guid userId, int currentCourseCount, CancellationToken cancellationToken)
        {
            EnsureCanCreateCourseCallCount++;
            LastUserId = userId;
            LastCurrentCourseCount = currentCourseCount;
            return Task.CompletedTask;
        }
    }
}
