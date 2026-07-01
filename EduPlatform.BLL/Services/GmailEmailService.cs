using System.Globalization;
using System.Net;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EduPlatform.BLL.Services;

public sealed class GmailEmailService(IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public Task SendAccountCreatedAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(recipientName);
        var body = $"""
            <h1>Chào mừng đến EduPlatform</h1>
            <p>Xin chào {safeName}, tài khoản của bạn đã được tạo thành công.</p>
            <p>Bạn có thể đăng nhập và bắt đầu sử dụng nền tảng.</p>
            """;

        return SendAsync(
            recipientEmail,
            recipientName,
            "Tài khoản EduPlatform đã được tạo",
            body,
            cancellationToken);
    }

    public Task SendCourseInvitationAsync(
        string recipientEmail,
        string recipientName,
        string courseTitle,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeCourseTitle = WebUtility.HtmlEncode(courseTitle);
        var body = $"""
            <h1>Bạn có lời mời tham gia khóa học</h1>
            <p>Xin chào {safeName}, bạn được mời tham gia khóa học <strong>{safeCourseTitle}</strong>.</p>
            <p>Đăng nhập EduPlatform để chấp nhận hoặc từ chối lời mời.</p>
            """;

        return SendAsync(
            recipientEmail,
            recipientName,
            "Lời mời tham gia khóa học",
            body,
            cancellationToken);
    }

    public Task SendPaymentConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string packageName,
        decimal amount,
        string transactionReference,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(recipientName);
        var safePackageName = WebUtility.HtmlEncode(packageName);
        var safeReference = WebUtility.HtmlEncode(transactionReference);
        var formattedAmount = amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        var body = $"""
            <h1>Thanh toán thành công</h1>
            <p>Xin chào {safeName}, thanh toán cho gói <strong>{safePackageName}</strong> đã được xác nhận.</p>
            <p>Số tiền: <strong>{formattedAmount} VND</strong></p>
            <p>Mã giao dịch: <strong>{safeReference}</strong></p>
            """;

        return SendAsync(
            recipientEmail,
            recipientName,
            "Xác nhận thanh toán EduPlatform",
            body,
            cancellationToken);
    }

    private async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            socketOptions,
            cancellationToken);
        await client.AuthenticateAsync(
            _options.Username,
            _options.AppPassword,
            cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || _options.Port is <= 0 or > 65535
            || string.IsNullOrWhiteSpace(_options.FromAddress)
            || string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.AppPassword))
        {
            throw new InvalidOperationException(
                "Email configuration is incomplete. Use environment variables or user secrets.");
        }
    }
}
