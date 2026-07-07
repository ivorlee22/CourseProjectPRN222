using EduPlatform.DAL.Entities;
using Pgvector;

namespace EduPlatform.DAL.Repositories;

public sealed record RetrievedDocumentChunk(
    DocumentChunk Chunk,
    string DocumentName,
    double SimilarityScore);

public interface IChatRepository
{
    Task<bool> CanAccessCourseAsync(
        Guid courseId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken);

    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSession>> ListSessionsAsync(
        Guid userId,
        Guid? courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Message>> ListMessagesAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Message>> ListRecentMessagesAsync(
        Guid sessionId,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RetrievedDocumentChunk>> SearchChunksAsync(
        Guid courseId,
        Vector queryEmbedding,
        int limit,
        CancellationToken cancellationToken);

    Task AddSessionAsync(ChatSession session, CancellationToken cancellationToken);

    Task AddMessagesAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);

    Task AddRetrievalLogsAsync(
        IEnumerable<RetrievalLog> retrievalLogs,
        CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

    void RemoveSession(ChatSession session);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
