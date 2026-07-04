using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using System.Text;

namespace EduPlatform.BLL.Services.TextExtractors;

public sealed class DocxTextExtractor : ITextExtractor
{
    public DocumentFileType FileType => DocumentFileType.Docx;

    public bool Supports(string contentType, string fileName)
    {
        if (string.Equals(
                contentType,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = new MemoryStream();
        content.CopyTo(buffer);
        buffer.Position = 0;

        try
        {
            using var document = WordprocessingDocument.Open(buffer, false);
            var documentPart = document.MainDocumentPart
                ?? throw new DocumentProcessingException(
                    "Tệp DOCX không có phần nội dung chính.");

            var body = documentPart.Document?.Body;
            if (body is null)
            {
                throw new DocumentProcessingException(
                    "Tệp DOCX không có phần nội dung chính.");
            }

            var builder = new StringBuilder();
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var paragraphText = paragraph.InnerText.Trim();
                if (paragraphText.Length == 0)
                {
                    builder.AppendLine();
                    continue;
                }

                builder.AppendLine(paragraphText);
            }

            var extracted = builder.ToString().Trim();
            return Task.FromResult<IReadOnlyList<ExtractedPage>>(
                extracted.Length == 0
                    ? []
                    : [new ExtractedPage(1, extracted)]);
        }
        catch (DocumentProcessingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DocumentProcessingException(
                "Không thể trích xuất nội dung từ tệp DOCX.", ex);
        }
    }
}