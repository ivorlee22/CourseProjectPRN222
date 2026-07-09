using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace EduPlatform.Web.Helpers;

public static partial class ChatMarkdownRenderer
{
    public static IHtmlContent Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return HtmlString.Empty;
        }

        var builder = new StringBuilder();
        var activeList = ListKind.None;
        var paragraphOpen = false;
        var codeBlockOpen = false;
        var codeLanguage = string.Empty;

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.TrimEnd();
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
            {
                if (codeBlockOpen)
                {
                    builder.Append("</code></pre>");
                    codeBlockOpen = false;
                    codeLanguage = string.Empty;
                }
                else
                {
                    CloseParagraph(builder, ref paragraphOpen);
                    CloseList(builder, ref activeList);
                    codeLanguage = trimmedLine[3..].Trim();
                    builder.Append("<pre class=\"chat-code-block\"><code");
                    if (!string.IsNullOrWhiteSpace(codeLanguage))
                    {
                        builder.Append(" data-code-language=\"");
                        builder.Append(WebUtility.HtmlEncode(codeLanguage));
                        builder.Append('"');
                    }
                    builder.Append('>');
                    codeBlockOpen = true;
                }

                continue;
            }

            if (codeBlockOpen)
            {
                builder.Append(WebUtility.HtmlEncode(rawLine));
                builder.Append('\n');
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                CloseParagraph(builder, ref paragraphOpen);
                CloseList(builder, ref activeList);
                continue;
            }

            var bulletMatch = BulletLinePattern().Match(line);
            if (bulletMatch.Success)
            {
                CloseParagraph(builder, ref paragraphOpen);
                OpenList(builder, ref activeList, ListKind.Unordered);
                builder.Append("<li>");
                builder.Append(RenderInline(bulletMatch.Groups[1].Value.Trim()));
                builder.Append("</li>");
                continue;
            }

            var numberMatch = NumberedLinePattern().Match(line);
            if (numberMatch.Success)
            {
                CloseParagraph(builder, ref paragraphOpen);
                OpenList(builder, ref activeList, ListKind.Ordered);
                builder.Append("<li>");
                builder.Append(RenderInline(numberMatch.Groups[1].Value.Trim()));
                builder.Append("</li>");
                continue;
            }

            CloseList(builder, ref activeList);
            if (paragraphOpen)
            {
                builder.Append("<br>");
            }
            else
            {
                builder.Append("<p>");
                paragraphOpen = true;
            }

            builder.Append(RenderInline(line.Trim()));
        }

        CloseParagraph(builder, ref paragraphOpen);
        CloseList(builder, ref activeList);
        if (codeBlockOpen)
        {
            builder.Append("</code></pre>");
        }

        return new HtmlString(builder.ToString());
    }

    private static string RenderInline(string value)
    {
        var encoded = WebUtility.HtmlEncode(value);
        encoded = InlineCodePattern().Replace(encoded, "<code>$1</code>");
        encoded = BoldPattern().Replace(encoded, "<strong>$1</strong>");
        return encoded;
    }

    private static void OpenList(StringBuilder builder, ref ListKind activeList, ListKind nextList)
    {
        if (activeList == nextList)
        {
            return;
        }

        CloseList(builder, ref activeList);
        builder.Append(nextList == ListKind.Ordered ? "<ol>" : "<ul>");
        activeList = nextList;
    }

    private static void CloseList(StringBuilder builder, ref ListKind activeList)
    {
        if (activeList == ListKind.None)
        {
            return;
        }

        builder.Append(activeList == ListKind.Ordered ? "</ol>" : "</ul>");
        activeList = ListKind.None;
    }

    private static void CloseParagraph(StringBuilder builder, ref bool paragraphOpen)
    {
        if (!paragraphOpen)
        {
            return;
        }

        builder.Append("</p>");
        paragraphOpen = false;
    }

    private enum ListKind
    {
        None,
        Unordered,
        Ordered
    }

    [GeneratedRegex(@"^\s*[-*]\s+(.+)$")]
    private static partial Regex BulletLinePattern();

    [GeneratedRegex(@"^\s*\d+[.)]\s+(.+)$")]
    private static partial Regex NumberedLinePattern();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldPattern();

    [GeneratedRegex(@"`([^`]+?)`")]
    private static partial Regex InlineCodePattern();
}
