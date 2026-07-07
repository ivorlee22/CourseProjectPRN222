using System;
using System.Threading;
using System.Threading.Tasks;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using EduPlatform.BLL.Models;

namespace EduPlatform.BLL.Services;

public sealed class SubscriptionChatQuotaService(
    ISubscriptionRepository subscriptionRepository,
    IPackageRepository packageRepository,
    IChatQuotaRepository chatQuotaRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider) : IChatQuotaService
{
    public async Task EnsureCanSendMessageAsync(Guid userId, CancellationToken cancellationToken)
    {
        // 1. Check if user is Admin, they can bypass the quota
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy người dùng.");
            
        if (user.Role == UserRole.Admin)
        {
            return;
        }

        // 2. Lock the user row to prevent race conditions during concurrent chat requests
        await chatQuotaRepository.LockUserRowAsync(userId, cancellationToken);

        // 3. Count messages sent today
        var utcNow = timeProvider.GetUtcNow();
        var startOfDay = new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        var messageCount = await chatQuotaRepository.CountMessagesTodayAsync(userId, startOfDay, cancellationToken);

        // 4. Get active subscription or fallback to Free package
        var subscription = await subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);
        var package = subscription?.Package ?? await GetFreePackageAsync(cancellationToken);

        // 5. Check if the quota is exceeded
        if (messageCount >= package.DailyChats)
        {
            throw new ChatQuotaExceededException(
                $"Bạn đã sử dụng hết {package.DailyChats} tin nhắn trong ngày. Hãy nâng cấp gói để tiếp tục.");
        }
    }

    private async Task<Package> GetFreePackageAsync(CancellationToken cancellationToken)
    {
        return await packageRepository.GetFreePackageAsync(cancellationToken)
            ?? throw new BusinessValidationException("Không tìm thấy gói Free để áp dụng quota mặc định.");
    }
}
