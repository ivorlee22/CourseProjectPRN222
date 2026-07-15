using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

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
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy người dùng.");

        if (user.Role == UserRole.Admin)
        {
            return;
        }

        await chatQuotaRepository.LockUserRowAsync(userId, cancellationToken);

        var utcNow = timeProvider.GetUtcNow();
        var startOfDay = new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        var messageCount = await chatQuotaRepository.CountMessagesTodayAsync(
            userId,
            startOfDay,
            cancellationToken);

        var subscription = await subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);
        var dailyChats = subscription is null
            ? (await GetFreePackageAsync(cancellationToken)).DailyChats
            : subscription.Package.DailyChats;

        if (messageCount >= dailyChats)
        {
            throw new ChatQuotaExceededException(
                $"Bạn đã sử dụng hết {dailyChats} tin nhắn trong ngày. Hãy nâng cấp gói để tiếp tục.");
        }
    }

    public async Task<(int MessageCount, int DailyLimit)> GetQuotaInfoAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy người dùng.");

        if (user.Role == UserRole.Admin || user.Role == UserRole.Teacher)
        {
            return (0, int.MaxValue);
        }

        var utcNow = timeProvider.GetUtcNow();
        var startOfDay = new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);
        var messageCount = await chatQuotaRepository.CountMessagesTodayAsync(
            userId,
            startOfDay,
            cancellationToken);

        var subscription = await subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);
        var dailyChats = subscription is null
            ? (await GetFreePackageAsync(cancellationToken)).DailyChats
            : subscription.Package.DailyChats;

        return (messageCount, dailyChats);
    }

    private async Task<Package> GetFreePackageAsync(CancellationToken cancellationToken)
    {
        return await packageRepository.GetFreePackageAsync(cancellationToken)
            ?? throw new BusinessValidationException("Không tìm thấy gói Free để áp dụng quota mặc định.");
    }

}
