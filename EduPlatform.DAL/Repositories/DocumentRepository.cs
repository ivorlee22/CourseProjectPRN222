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

    public async Task<IReadOnlyList<DocumentListItem>> ListByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DocumentListItem(
                x.Id,
                x.CourseId,
                x.Course.Title,
                x.OriginalFileName,
                x.SizeBytes,
                x.Status,
                x.FailureReason,
                x.Chunks.Count,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
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
