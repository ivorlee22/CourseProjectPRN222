using EduPlatform.BLL.DTOs.Documents;
using EduPlatform.BLL.Models;

namespace EduPlatform.BLL.Interfaces;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentSummaryDto>> ListByCourseAsync(
        Guid courseId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<DocumentDetailsDto> GetByIdAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentChunkDto>> ListChunksAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<float>?> GetChunkEmbeddingAsync(
        Guid documentId,
        Guid chunkId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<Guid> UploadAsync(
        UploadDocumentCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<string> GetDownloadUrlAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken);
}