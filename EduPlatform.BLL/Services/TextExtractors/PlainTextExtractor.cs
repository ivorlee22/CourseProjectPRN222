using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;

namespace EduPlatform.BLL.Services.TextExtractors;

public sealed class PlainTextExtractor : ITextExtractor
{
    public DocumentFileType FileType { get; }

    public PlainTextExtractor(DocumentFileType fileType)
    {
        if (fileType is not (DocumentFileType.Txt or DocumentFileType.Md))
        {
            throw new ArgumentException(
                $"{nameof(PlainTextExtractor)} chỉ hỗ trợ TXT và Markdown.",
                nameof(fileType));
        }

        FileType = fileType;
    }

    public bool Supports(string contentType, string fileName)
    {
        return fileTypeMatches(fileName);
    }

    public async Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            var text = (await reader.ReadToEndAsync(cancellationToken)).Trim();

            return text.Length == 0
                ? []
                : [new ExtractedPage(1, text)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DocumentProcessingException(
                "Không thể đọc nội dung tệp văn bản.", ex);
        }
    }

    private bool fileTypeMatches(string fileName)
    {
        return FileType switch
        {
            DocumentFileType.Txt => fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase),
            DocumentFileType.Md => fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}