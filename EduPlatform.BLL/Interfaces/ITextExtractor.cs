using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace EduPlatform.BLL.Interfaces;

/// <summary>
/// Reads plain text out of an uploaded document stream. Concrete implementations
/// live in the BLL and choose libraries for each supported file type.
/// </summary>
public interface ITextExtractor
{
    /// <summary>True when this extractor supports the supplied content type/extension.</summary>
    bool Supports(string contentType, string fileName);

    /// <summary>Returns the file-type enum handled by this extractor.</summary>
    DocumentFileType FileType { get; }

    /// <summary>
    /// Extracts the textual content of the document. The returned
    /// <see cref="ExtractedPage"/> sequence preserves page boundaries when the
    /// format exposes them.
    /// </summary>
    Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken);
}

public sealed record ExtractedPage(int PageNumber, string Text);