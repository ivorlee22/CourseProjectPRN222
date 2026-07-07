using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class ChatRepository(AppDbContext dbContext) : IChatRepository
{
    public Task<bool> CanAccessCourseAsync(
        Guid courseId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        return dbContext.Courses.AnyAsync(
            course => course.Id == courseId
                && (isAdmin
                    || course.OwnerId == userId
                    || course.Enrollments.Any(enrollment =>
                        enrollment.UserId == userId
                        && enrollment.Status == EnrollmentStatus.Active)),
            cancellationToken);
    }

    public Task<ChatSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return dbContext.ChatSessions.SingleOrDefaultAsync(
            session => session.Id == sessionId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> ListSessionsAsync(
        Guid userId,
        Guid? courseId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ChatSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId);

        if (courseId.HasValue)
        {
            query = query.Where(session => session.CourseId == courseId.Value);
        }

        return await query
            .OrderByDescending(session => session.LastMessageAtUtc ?? session.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> ListMessagesAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ChatSessionId == sessionId)
            .Include(message => message.RetrievalLogs)
                .ThenInclude(log => log.DocumentChunk)
                    .ThenInclude(chunk => chunk.Document)
            .AsSplitQuery()
            .OrderBy(message => message.CreatedAtUtc)
            .ThenBy(message => message.Role == MessageRole.User ? 0 : 1)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> ListRecentMessagesAsync(
        Guid sessionId,
        int limit,
        CancellationToken cancellationToken)
    {
        var messages = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ChatSessionId == sessionId)
            .OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Role == MessageRole.Assistant ? 1 : 0)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return messages
            .OrderBy(message => message.CreatedAtUtc)
            .ThenBy(message => message.Role == MessageRole.User ? 0 : 1)
            .ToArray();
    }

    public async Task<IReadOnlyList<RetrievedDocumentChunk>> SearchChunksAsync(
        Guid courseId,
        Vector queryEmbedding,
        int limit,
        CancellationToken cancellationToken)
    {
        var results = await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk =>
                chunk.Document.CourseId == courseId
                && chunk.Document.Status == DocumentStatus.Ready
                && chunk.Embedding != null)
            .Select(chunk => new
            {
                Chunk = chunk,
                DocumentName = chunk.Document.OriginalFileName,
                Distance = chunk.Embedding!.CosineDistance(queryEmbedding)
            })
            .OrderBy(item => item.Distance)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return results
            .Select(item => new RetrievedDocumentChunk(
                item.Chunk,
                item.DocumentName,
                Math.Clamp(1d - item.Distance, -1d, 1d)))
            .ToArray();
    }

    public Task AddSessionAsync(ChatSession session, CancellationToken cancellationToken)
    {
        return dbContext.ChatSessions.AddAsync(session, cancellationToken).AsTask();
    }

    public Task AddMessagesAsync(
        IEnumerable<Message> messages,
        CancellationToken cancellationToken)
    {
        return dbContext.Messages.AddRangeAsync(messages, cancellationToken);
    }

    public Task AddRetrievalLogsAsync(
        IEnumerable<RetrievalLog> retrievalLogs,
        CancellationToken cancellationToken)
    {
        return dbContext.RetrievalLogs.AddRangeAsync(retrievalLogs, cancellationToken);
    }

    public void RemoveSession(ChatSession session)
    {
        dbContext.ChatSessions.Remove(session);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
