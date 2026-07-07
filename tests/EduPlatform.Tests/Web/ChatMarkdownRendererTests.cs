using System.Text.Encodings.Web;
using EduPlatform.Web.Helpers;

namespace EduPlatform.Tests.Web;

[TestClass]
public sealed class ChatMarkdownRendererTests
{
    [TestMethod]
    public void Render_BoldMarkdown_EmitsStrongTag()
    {
        var html = Render("Đây là **nội dung quan trọng**.");

        Assert.Contains("<strong>nội dung quan trọng</strong>", html);
        Assert.DoesNotContain("**nội dung quan trọng**", html);
    }

    [TestMethod]
    public void Render_BulletMarkdown_EmitsList()
    {
        var html = Render("""
            * **RQ1:** Nội dung một
            * **RQ2:** Nội dung hai
            """);

        Assert.Contains("<ul>", html);
        Assert.Contains("<li><strong>RQ1:</strong> Nội dung một</li>", html);
        Assert.Contains("<li><strong>RQ2:</strong> Nội dung hai</li>", html);
    }

    [TestMethod]
    public void Render_HtmlInput_EscapesHtml()
    {
        var html = Render("<script>alert(1)</script> **safe**");

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("<strong>safe</strong>", html);
    }

    private static string Render(string markdown)
    {
        using var writer = new StringWriter();
        ChatMarkdownRenderer.Render(markdown).WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
