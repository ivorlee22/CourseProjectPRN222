using EduPlatform.BLL.DTOs.Courses;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using BllCourseType = EduPlatform.BLL.Enums.CourseType;
using BllEnrollmentStatus = EduPlatform.BLL.Enums.EnrollmentStatus;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;
using DalCourseType = EduPlatform.DAL.Entities.CourseType;
using DalEnrollmentStatus = EduPlatform.DAL.Entities.EnrollmentStatus;

namespace EduPlatform.BLL.Services;

public sealed class CourseService(
    ICourseRepository courseRepository,
    ICourseQuotaService courseQuotaService,
    TimeProvider timeProvider) : ICourseService
{
    private const int MaximumPageSize = 50;

    public async Task<PagedResult<CourseSummaryDto>> SearchAsync(
        CourseSearchQuery query,
        ActorContext? actor,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        
        Guid? ownerId = null;
        Guid? enrolledUserId = null;
        
        if (query.MineOnly)
        {
            if (actor is null)
            {
                throw new ForbiddenOperationException("Bạn cần đăng nhập để xem khóa học của mình.");
            }

            if (actor.Role == EduPlatform.BLL.Enums.UserRole.Student)
            {
                enrolledUserId = actor.UserId;
            }
            else
            {
                ownerId = actor.UserId;
            }
        }
        
        var visibleOnly = !query.IncludeHidden && (actor is null || !actor.IsAdmin);

        if (query.MineOnly)
        {
            visibleOnly = false;
        }

        var result = await courseRepository.SearchAsync(
            query.Keyword,
            pageNumber,
            pageSize,
            visibleOnly,
            ownerId,
            enrolledUserId,
            cancellationToken);

        return new PagedResult<CourseSummaryDto>(
            result.Items.Select(MapSummary).ToArray(),
            pageNumber,
            pageSize,
            result.TotalCount);
    }

    public async Task<CourseDetailsDto> GetByIdAsync(
        Guid id,
        ActorContext? actor,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, cancellationToken);

        var isEnrolled = actor is not null
            && course.Enrollments.Any(x =>
                x.UserId == actor.UserId
                && x.Status == DalEnrollmentStatus.Active);

        if (!course.IsVisible && !CanManage(course, actor))
        {
            if (!isEnrolled)
            {
                throw new ResourceNotFoundException("Không tìm thấy khóa học.");
            }
        }

        return MapDetails(course, isEnrolled);
    }

    public async Task<Guid> CreateAsync(
        CreateCourseCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        EnsureCanCreate(actor);
        ValidateCourse(command.Title, command.Description, command.Type, command.EnrollmentPassword);

        var ownerId = command.OwnerId ?? actor.UserId;

        var course = new Course
        {
            OwnerId = ownerId,
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            Type = ToDal(command.Type),
            IsVisible = command.Type == BllCourseType.Public,
            EnrollmentPasswordHash = HashPassword(command.Type, command.EnrollmentPassword)
        };

        await courseRepository.AddAsync(course, cancellationToken);
        await courseRepository.SaveChangesAsync(cancellationToken);

        return course.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateCourseCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        ValidateCourse(command.Title, command.Description, command.Type, command.EnrollmentPassword);

        var course = await GetCourseAsync(id, cancellationToken);
        EnsureCanAdminister(actor);

        course.Title = command.Title.Trim();
        course.Description = command.Description.Trim();
        course.Type = ToDal(command.Type);
        course.IsVisible = command.Type == BllCourseType.Public;

        if (actor.IsAdmin && command.OwnerId.HasValue)
        {
            course.OwnerId = command.OwnerId.Value;
        }

        if (command.Type == BllCourseType.Private && command.RemoveEnrollmentPassword)
        {
            throw new BusinessValidationException(
                "Hãy chuyển khóa học sang công khai trước khi xóa mật khẩu.");
        }

        if (command.Type == BllCourseType.Private
            && course.EnrollmentPasswordHash is null
            && string.IsNullOrWhiteSpace(command.EnrollmentPassword))
        {
            throw new BusinessValidationException(
                "Khóa học riêng tư cần mật khẩu tham gia.");
        }

        if (command.Type == BllCourseType.Public)
        {
            course.EnrollmentPasswordHash = null;
        }
        else if (!string.IsNullOrWhiteSpace(command.EnrollmentPassword))
        {
            course.EnrollmentPasswordHash = BCrypt.Net.BCrypt.HashPassword(
                command.EnrollmentPassword);
        }

        await courseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, cancellationToken);
        EnsureCanAdminister(actor);

        courseRepository.Remove(course);
        await courseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetVisibilityAsync(
        Guid id,
        bool isVisible,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, cancellationToken);
        EnsureCanAdminister(actor);

        course.IsVisible = isVisible;
        await courseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task EnrollAsync(
        Guid id,
        string? enrollmentPassword,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, cancellationToken);

        if (course.OwnerId == actor.UserId)
        {
            throw new ResourceConflictException("Bạn đang là chủ sở hữu khóa học.");
        }

        var existingEnrollment = await courseRepository.GetEnrollmentAsync(
            id,
            actor.UserId,
            cancellationToken);

        if (existingEnrollment?.Status == DalEnrollmentStatus.Active)
        {
            throw new ResourceConflictException("Bạn đã tham gia khóa học này.");
        }

        VerifyEnrollmentPassword(course, enrollmentPassword);
        await EnsureCanJoinAdditionalCourseAsync(actor.UserId, cancellationToken);

        if (existingEnrollment is null)
        {
            await courseRepository.AddEnrollmentAsync(
                new CourseEnrollment
                {
                    CourseId = id,
                    UserId = actor.UserId,
                    Status = DalEnrollmentStatus.Active,
                    EnrolledAtUtc = timeProvider.GetUtcNow()
                },
                cancellationToken);
        }
        else
        {
            existingEnrollment.Status = DalEnrollmentStatus.Active;
            existingEnrollment.EnrolledAtUtc = timeProvider.GetUtcNow();
        }

        await courseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> InviteAsync(
        Guid id,
        string studentLookup,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, cancellationToken);
        EnsureCanManage(course, actor);

        if (string.IsNullOrWhiteSpace(studentLookup))
        {
            throw new BusinessValidationException("Vui lòng nhập email hoặc tên học viên.");
        }

        var students = await courseRepository.FindActiveStudentsByEmailOrNameAsync(
            studentLookup,
            cancellationToken);

        if (students.Count == 0)
        {
            throw new ResourceNotFoundException("Không tìm thấy học viên theo email hoặc tên đã nhập.");
        }

        if (students.Count > 1)
        {
            throw new BusinessValidationException("Có nhiều học viên trùng tên. Vui lòng mời bằng email.");
        }

        var userId = students[0].Id;

        if (userId == course.OwnerId)
        {
            throw new ResourceConflictException("Chủ sở hữu đã thuộc khóa học.");
        }

        var existingEnrollment = await courseRepository.GetEnrollmentAsync(
            id,
            userId,
            cancellationToken);

        if (existingEnrollment is not null)
        {
            throw new ResourceConflictException(
                "Người dùng đã tham gia hoặc đã có lời mời.");
        }

        await courseRepository.AddEnrollmentAsync(
            new CourseEnrollment
            {
                CourseId = id,
                UserId = userId,
                Status = DalEnrollmentStatus.Pending,
                InvitedById = actor.UserId
            },
            cancellationToken);

        await courseRepository.SaveChangesAsync(cancellationToken);
        return userId;
    }

    public async Task RespondToInvitationAsync(
        Guid id,
        bool accept,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var enrollment = await courseRepository.GetEnrollmentAsync(
            id,
            actor.UserId,
            cancellationToken);

        if (enrollment is null || enrollment.Status != DalEnrollmentStatus.Pending)
        {
            throw new ResourceNotFoundException("Không tìm thấy lời mời đang chờ.");
        }

        if (accept)
        {
            await EnsureCanJoinAdditionalCourseAsync(actor.UserId, cancellationToken);
        }

        enrollment.Status = accept
            ? DalEnrollmentStatus.Active
            : DalEnrollmentStatus.Rejected;
        enrollment.EnrolledAtUtc = accept ? timeProvider.GetUtcNow() : null;

        await courseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelInvitationAsync(
        Guid courseId,
        Guid userId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(courseId, cancellationToken);
        EnsureCanManage(course, actor);

        var enrollment = await courseRepository.GetEnrollmentAsync(
            courseId,
            userId,
            cancellationToken);

        if (enrollment is null || enrollment.Status != DalEnrollmentStatus.Pending)
        {
            throw new ResourceNotFoundException("Không tìm thấy lời mời đang chờ.");
        }

        courseRepository.RemoveEnrollment(enrollment);
        await courseRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CourseInvitationDto>> GetPendingInvitationsAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var invitations = await courseRepository.GetPendingInvitationsAsync(
            actor.UserId,
            cancellationToken);

        return invitations
            .Select(x => new CourseInvitationDto(
                x.CourseId,
                x.CourseTitle,
                x.InviterName))
            .ToArray();
    }

    public Task<int> CountPendingInvitationsAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        return courseRepository.CountPendingInvitationsAsync(actor.UserId, cancellationToken);
    }

    private async Task EnsureCanJoinAdditionalCourseAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var currentActiveCourseCount = await courseRepository.CountActiveEnrollmentsByUserAsync(
            userId,
            cancellationToken);

        await courseQuotaService.EnsureCanJoinCourseAsync(
            userId,
            currentActiveCourseCount,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CourseStudentDto>> GetStudentsAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseAsync(id, cancellationToken);
        EnsureCanManage(course, actor);

        var students = await courseRepository.GetStudentsAsync(id, cancellationToken);

        return students
            .Select(x => new CourseStudentDto(
                x.UserId,
                x.User.FullName,
                x.User.Email,
                ToBll(x.Status),
                x.EnrolledAtUtc))
            .ToArray();
    }

    private async Task<Course> GetCourseAsync(Guid id, CancellationToken cancellationToken)
    {
        return await courseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy khóa học.");
    }

    private static CourseSummaryDto MapSummary(Course course)
    {
        return new CourseSummaryDto(
            course.Id,
            course.Title,
            course.Description,
            ToBll(course.Type),
            course.IsVisible,
            course.OwnerId,
            course.Owner.FullName,
            ToBll(course.Owner.Role),
            course.Enrollments.Count(x => x.Status == DalEnrollmentStatus.Active),
            course.CreatedAtUtc);
    }

    private static CourseDetailsDto MapDetails(Course course, bool isEnrolled)
    {
        return new CourseDetailsDto(
            course.Id,
            course.Title,
            course.Description,
            ToBll(course.Type),
            course.IsVisible,
            course.EnrollmentPasswordHash is not null,
            course.OwnerId,
            course.Owner.FullName,
            ToBll(course.Owner.Role),
            course.Enrollments.Count(x => x.Status == DalEnrollmentStatus.Active),
            course.CreatedAtUtc,
            course.UpdatedAtUtc,
            isEnrolled);
    }

    private static void ValidateCourse(
        string title,
        string description,
        BllCourseType type,
        string? enrollmentPassword)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
        {
            throw new BusinessValidationException(
                "Tên khóa học phải có từ 1 đến 200 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 4000)
        {
            throw new BusinessValidationException(
                "Mô tả khóa học phải có từ 1 đến 4000 ký tự.");
        }

        if (type == BllCourseType.Private
            && enrollmentPassword is not null
            && enrollmentPassword.Length is < 6 or > 72)
        {
            throw new BusinessValidationException(
                "Mật khẩu tham gia phải có từ 6 đến 72 ký tự.");
        }
    }

    private static void VerifyEnrollmentPassword(Course course, string? enrollmentPassword)
    {
        if (course.Type != DalCourseType.Private)
        {
            return;
        }

        if (course.EnrollmentPasswordHash is null)
        {
            throw new BusinessValidationException(
                "Khóa học riêng tư chưa được cấu hình mật khẩu.");
        }

        if (string.IsNullOrWhiteSpace(enrollmentPassword)
            || !BCrypt.Net.BCrypt.Verify(
                enrollmentPassword,
                course.EnrollmentPasswordHash))
        {
            throw new BusinessValidationException("Mật khẩu tham gia không đúng.");
        }
    }

    private static void EnsureCanCreate(ActorContext actor)
    {
        if (!actor.IsAdmin)
        {
            throw new ForbiddenOperationException(
                "Chi Quan tri vien moi co quyen tao khoa hoc.");
        }
    }

    private static void EnsureCanAdminister(ActorContext actor)
    {
        if (!actor.IsAdmin)
        {
            throw new ForbiddenOperationException(
                "Chỉ quản trị viên mới có quyền chỉnh sửa, ẩn hoặc xóa khóa học.");
        }
    }

    private static bool CanManage(Course course, ActorContext? actor)
    {
        return actor is not null && (actor.IsAdmin || course.OwnerId == actor.UserId);
    }

    private static void EnsureCanManage(Course course, ActorContext actor)
    {
        if (!CanManage(course, actor))
        {
            throw new ForbiddenOperationException(
                "Bạn không có quyền quản lý khóa học này.");
        }
    }

    private static string? HashPassword(
        BllCourseType type,
        string? enrollmentPassword)
    {
        if (type == BllCourseType.Public)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(enrollmentPassword))
        {
            throw new BusinessValidationException(
                "Khóa học riêng tư cần mật khẩu tham gia.");
        }

        if (enrollmentPassword.Length is < 6 or > 72)
        {
            throw new BusinessValidationException(
                "Mật khẩu tham gia phải có từ 6 đến 72 ký tự.");
        }

        return BCrypt.Net.BCrypt.HashPassword(enrollmentPassword);
    }

    private static DalCourseType ToDal(BllCourseType type)
    {
        return type switch
        {
            BllCourseType.Public => DalCourseType.Public,
            BllCourseType.Private => DalCourseType.Private,
            _ => throw new BusinessValidationException("Loại khóa học không hợp lệ.")
        };
    }

    private static BllCourseType ToBll(DalCourseType type)
    {
        return type switch
        {
            DalCourseType.Public => BllCourseType.Public,
            DalCourseType.Private => BllCourseType.Private,
            _ => throw new InvalidOperationException("Unsupported persisted course type.")
        };
    }

    private static BllUserRole ToBll(UserRole role)
    {
        return role switch
        {
            UserRole.Student => BllUserRole.Student,
            UserRole.Teacher => BllUserRole.Teacher,
            UserRole.Admin => BllUserRole.Admin,
            _ => throw new InvalidOperationException("Unsupported persisted user role.")
        };
    }

    private static BllEnrollmentStatus ToBll(DalEnrollmentStatus status)
    {
        return status switch
        {
            DalEnrollmentStatus.Pending => BllEnrollmentStatus.Pending,
            DalEnrollmentStatus.Active => BllEnrollmentStatus.Active,
            DalEnrollmentStatus.Rejected => BllEnrollmentStatus.Rejected,
            _ => throw new InvalidOperationException(
                "Unsupported persisted enrollment status.")
        };
    }
}
