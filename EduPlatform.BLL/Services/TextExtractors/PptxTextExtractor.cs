using DocumentFormat.OpenXml.Packaging;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using System.Text;

namespace EduPlatform.BLL.Services.TextExtractors;

/// <summary>
/// Extracts text from PowerPoint (.pptx) files. Each slide becomes a single
/// <see cref="ExtractedPage"/> so callers can preserve slide boundaries while
/// chunking. The implementation uses the Open XML SDK that is already shipped
/// for DOCX support, so no new dependency is required.
/// </summary>
public sealed class PptxTextExtractor : ITextExtractor
{
    public DocumentFileType FileType => DocumentFileType.Pptx;

    public bool Supports(string contentType, string fileName)
    {
        if (string.Equals(
                contentType,
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);
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
            using var document = PresentationDocument.Open(buffer, false);
            var presentationPart = document.PresentationPart
                ?? throw new DocumentProcessingException(
                    "Tệp PPTX không có phần nội dung chính.");

            var presentation = presentationPart.Presentation
                ?? throw new DocumentProcessingException(
                    "Tệp PPTX không có phần nội dung chính.");

            var slides = presentation.SlideIdList?
                .Elements<DocumentFormat.OpenXml.Presentation.SlideId>()
                .ToList() ?? [];

            if (slides.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<ExtractedPage>>([]);
            }

            var pages = new List<ExtractedPage>(slides.Count);
            for (var index = 0; index < slides.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var slideId = slides[index];
                var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                var slideText = ExtractSlideText(slidePart);
                if (slideText.Length == 0)
                {
                    continue;
                }

                pages.Add(new ExtractedPage(index + 1, slideText));
            }

            return Task.FromResult<IReadOnlyList<ExtractedPage>>(pages);
        }
        catch (DocumentProcessingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DocumentProcessingException(
                "Không thể trích xuất nội dung từ tệp PPTX.", ex);
        }
    }

    private static string ExtractSlideText(SlidePart slidePart)
    {
        var slide = slidePart.Slide
            ?? throw new DocumentProcessingException(
                "Tệp PPTX có slide thiếu nội dung.");

        var builder = new StringBuilder();
        foreach (var paragraph in slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = paragraph.InnerText.Trim();
            if (text.Length == 0)
            {
                builder.AppendLine();
                continue;
            }

            builder.AppendLine(text);
        }

        return builder.ToString().Trim();
    }
}