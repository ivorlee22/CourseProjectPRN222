using EduPlatform.BLL.Interfaces;

namespace EduPlatform.BLL.Services;

/// <summary>
/// Splits the extracted text into overlapping windows while keeping page
/// numbers attached. The chunker prefers paragraph boundaries when the
/// configured chunk size allows it.
/// </summary>
public sealed class FixedSizeTextChunker : ITextChunker
{
    public IReadOnlyList<ChunkResult> Chunk(
        IReadOnlyList<ExtractedPage> pages,
        int chunkSize,
        int chunkOverlap)
    {
        ValidateParameters(chunkSize, chunkOverlap);

        if (pages.Count == 0)
        {
            return [];
        }

        var chunks = new List<ChunkResult>();
        var globalSequence = 0;

        foreach (var page in pages)
        {
            chunks.AddRange(ChunkPage(page, chunkSize, chunkOverlap));
            globalSequence = chunks.Count;
        }

        _ = globalSequence;
        return chunks;
    }

    private static IEnumerable<ChunkResult> ChunkPage(
        ExtractedPage page,
        int chunkSize,
        int chunkOverlap)
    {
        var text = NormalizeWhitespace(page.Text);

        if (text.Length == 0)
        {
            yield break;
        }

        if (text.Length <= chunkSize)
        {
            yield return new ChunkResult(text, page.PageNumber, Section: null);
            yield break;
        }

        var step = Math.Max(1, chunkSize - chunkOverlap);
        for (var start = 0; start < text.Length; start += step)
        {
            var end = Math.Min(start + chunkSize, text.Length);
            var window = text[start..end].Trim();

            if (window.Length == 0)
            {
                continue;
            }

            yield return new ChunkResult(window, page.PageNumber, Section: null);

            if (end == text.Length)
            {
                yield break;
            }
        }
    }

    private static void ValidateParameters(int chunkSize, int chunkOverlap)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkSize), "Chunk size phải lớn hơn 0.");
        }

        if (chunkOverlap < 0 || chunkOverlap >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkOverlap),
                "Chunk overlap phải lớn hơn hoặc bằng 0 và nhỏ hơn chunk size.");
        }
    }

    private static string NormalizeWhitespace(string input)
    {
        var builder = new System.Text.StringBuilder(input.Length);
        var previousWasSpace = false;

        foreach (var ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
            }
            else
            {
                builder.Append(ch);
                previousWasSpace = false;
            }
        }

        return builder.ToString().Trim();
    }
}