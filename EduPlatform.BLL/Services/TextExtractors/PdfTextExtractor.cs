using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using iText.Kernel.Pdf;
using iTextTextExtractor = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor;

namespace EduPlatform.BLL.Services.TextExtractors;

public sealed class PdfTextExtractor : ITextExtractor
{
    public DocumentFileType FileType => DocumentFileType.Pdf;

    public bool Supports(string contentType, string fileName)
    {
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // iText7 reads from a non-seekable stream via a copy. Buffer the
        // incoming stream to disk-free memory so we can re-open it as a PdfReader.
        var buffer = new MemoryStream();
        content.CopyTo(buffer);
        buffer.Position = 0;

        try
        {
            using var reader = new PdfReader(buffer);
            using var pdf = new PdfDocument(reader);

            var pages = new List<ExtractedPage>(pdf.GetNumberOfPages());
            for (var i = 1; i <= pdf.GetNumberOfPages(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = iTextTextExtractor
                    .GetTextFromPage(pdf.GetPage(i))
                    .Trim();

                if (text.Length == 0)
                {
                    continue;
                }

                pages.Add(new ExtractedPage(i, text));
            }

            return Task.FromResult<IReadOnlyList<ExtractedPage>>(pages);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new DocumentProcessingException(
                "Không thể trích xuất nội dung từ tệp PDF.", ex);
        }
    }
}