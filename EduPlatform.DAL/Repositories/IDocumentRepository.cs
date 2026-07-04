using EduPlatform.DAL.Entities;

namespace EduPlatform.DAL.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> ListByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentChunk>> ListChunksAsync(
        Guid documentId,
        CancellationToken cancellationToken);

    Task AddAsync(Document document, CancellationToken cancellationToken);

    Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken);

    void Remove(Document document);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}