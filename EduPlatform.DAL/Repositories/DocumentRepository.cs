using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class DocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Documents
            .Include(x => x.Course)
            .Include(x => x.Chunks)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> ListByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Chunks)
            .Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByCourseAndFileNameAsync(
        Guid courseId,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        return dbContext.Documents
            .AsNoTracking()
            .AnyAsync(x => x.CourseId == courseId
                && x.OriginalFileName == originalFileName,
                cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> ListChunksAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        return dbContext.Documents.AddAsync(document, cancellationToken).AsTask();
    }

    public Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        return dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
    }

    public void Remove(Document document)
    {
        dbContext.Documents.Remove(document);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}