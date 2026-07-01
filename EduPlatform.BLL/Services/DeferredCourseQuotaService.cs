using EduPlatform.BLL.Interfaces;

namespace EduPlatform.BLL.Services;

/// <summary>
/// Temporary no-op implementation used only while the Subscription module is unavailable.
/// It deliberately allows every course creation request and does not enforce package limits.
/// Nguyên must replace this class with a subscription-backed implementation of
/// <see cref="ICourseQuotaService"/> before subscription-limit acceptance testing.
/// See AGENTS.md, section "Nguyên Handoff: Course Quota Integration".
/// </summary>
public sealed class DeferredCourseQuotaService : ICourseQuotaService
{
    public Task EnsureCanCreateCourseAsync(
        Guid userId,
        int currentCourseCount,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
