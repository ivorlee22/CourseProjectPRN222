using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class ChatQuotaRepository(AppDbContext dbContext) : IChatQuotaRepository
{
    public async Task<int> CountMessagesTodayAsync(Guid userId, DateTimeOffset startOfDay, CancellationToken cancellationToken)
    {
        return await dbContext.Messages
            .Where(m => m.Role == MessageRole.User && 
                        m.CreatedAtUtc >= startOfDay &&
                        m.ChatSession.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task LockUserRowAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Using raw SQL to lock the User row within the current transaction
        // This ensures concurrent requests for the same user will wait here
        // preventing race conditions when counting messages.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE", 
            cancellationToken);
    }
}
