using EduPlatform.BLL.Interfaces;

namespace EduPlatform.BLL.Services;

/// <summary>
/// Splits the extracted text into overlapping chunks while keeping page
/// numbers attached. The chunker prefers paragraph, line, and sentence
/// boundaries before falling back to a fixed character window so that
/// chunks stay coherent and aligned with the source document's natural
/// structure.
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
        foreach (var page in pages)
        {
            chunks.AddRange(ChunkPage(page, chunkSize, chunkOverlap));
        }

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

        var buffer = new System.Text.StringBuilder();

        foreach (var segment in EnumerateSegments(text))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            AppendWithBoundary(buffer, segment);

            while (buffer.Length >= chunkSize)
            {
                // Tìm word boundary để không cắt ngang chữ
                var cutPoint = chunkSize;
                while (cutPoint > 0 && cutPoint < buffer.Length && buffer[cutPoint] != ' ')
                    cutPoint--;
                if (cutPoint == 0) cutPoint = chunkSize; // fallback nếu không tìm được space

                var chunkText = buffer.ToString(0, cutPoint).TrimEnd();
                if (chunkText.Length > 0)
                {
                    yield return new ChunkResult(chunkText, page.PageNumber, Section: null);
                }

                var remainAfterChunk = buffer.ToString(cutPoint, buffer.Length - cutPoint).TrimStart();
                var overlapStart = Math.Max(0, cutPoint - chunkOverlap);
                var carry = buffer.ToString(overlapStart, cutPoint - overlapStart);
                buffer.Clear();
                buffer.Append(carry);
                if (remainAfterChunk.Length > 0)
                {
                    AppendWithBoundary(buffer, remainAfterChunk);
                }
            }
        }

        if (buffer.Length > 0)
        {
            var final = buffer.ToString().TrimEnd();
            if (final.Length > 0)
            {
                yield return new ChunkResult(final, page.PageNumber, Section: null);
            }
        }
    }

    private static void AppendWithBoundary(System.Text.StringBuilder buffer, string segment)
    {
        if (buffer.Length == 0)
        {
            buffer.Append(segment);
            return;
        }

        if (buffer[^1] != ' ')
        {
            buffer.Append(' ');
        }

        buffer.Append(segment);
    }

    private static IEnumerable<string> EnumerateSegments(string text)
    {
        var paragraphStart = 0;
        var i = 0;
        while (i < text.Length)
        {
            if (i + 1 < text.Length && text[i] == '\n' && text[i + 1] == '\n')
            {
                var segment = text[paragraphStart..i].Trim();
                if (segment.Length > 0)
                {
                    foreach (var piece in SplitSegment(segment))
                    {
                        yield return piece;
                    }
                }

                i += 2;
                paragraphStart = i;
                continue;
            }

            i++;
        }

        if (paragraphStart < text.Length)
        {
            var segment = text[paragraphStart..].Trim();
            if (segment.Length > 0)
            {
                foreach (var piece in SplitSegment(segment))
                {
                    yield return piece;
                }
            }
        }
    }

    private static IEnumerable<string> SplitSegment(string paragraph)
    {
        if (paragraph.Length == 0)
        {
            yield break;
        }

        var lineStart = 0;
        for (var i = 0; i < paragraph.Length; i++)
        {
            if (paragraph[i] == '\n')
            {
                var line = paragraph[lineStart..i].Trim();
                if (line.Length > 0)
                {
                    foreach (var sentence in SplitLineBySentences(line))
                    {
                        yield return sentence;
                    }
                }

                lineStart = i + 1;
            }
        }

        if (lineStart < paragraph.Length)
        {
            var line = paragraph[lineStart..].Trim();
            if (line.Length > 0)
            {
                foreach (var sentence in SplitLineBySentences(line))
                {
                    yield return sentence;
                }
            }
        }
    }

    private static IEnumerable<string> SplitLineBySentences(string line)
    {
        if (line.Length == 0)
        {
            yield break;
        }

        var sentenceStart = 0;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch is '.' or '!' or '?' or ';' or ',')
            {
                var isAbbreviation = ch == '.'
                    && i + 1 < line.Length
                    && char.IsLower(line[i + 1]);
                if (isAbbreviation)
                {
                    continue;
                }

                var sentenceEnd = i + 1;
                var sentence = line[sentenceStart..sentenceEnd].Trim();
                if (sentence.Length > 0)
                {
                    yield return sentence;
                }

                sentenceStart = sentenceEnd;
            }
        }

        if (sentenceStart < line.Length)
        {
            var tail = line[sentenceStart..].Trim();
            if (tail.Length > 0)
            {
                yield return tail;
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
        var normalized = input.Replace("\r\n", "\n").Replace("\r", "\n");
        var builder = new System.Text.StringBuilder(normalized.Length);
        var i = 0;

        while (i < normalized.Length)
        {
            if (i + 1 < normalized.Length && normalized[i] == '\n' && normalized[i + 1] == '\n')
            {
                while (builder.Length > 0 && builder[^1] == ' ')
                    builder.Length--;
                builder.Append("\n\n");
                i += 2;
                while (i < normalized.Length && normalized[i] == '\n')
                    i++;
                continue;
            }

            if (normalized[i] == '\n')
            {
                if (builder.Length > 0 && builder[^1] != ' ' && builder[^1] != '\n')
                    builder.Append(' ');
                i++;
                continue;
            }

            if (char.IsWhiteSpace(normalized[i]))
            {
                if (builder.Length > 0 && builder[^1] != ' ' && builder[^1] != '\n')
                    builder.Append(' ');
                i++;
                continue;
            }

            builder.Append(normalized[i]);
            i++;
        }

        return builder.ToString().Trim();
    }
}