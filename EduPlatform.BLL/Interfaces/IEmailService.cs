namespace EduPlatform.BLL.Interfaces;

public interface IEmailService
{
    Task SendAccountCreatedAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken);

    Task SendCourseInvitationAsync(
        string recipientEmail,
        string recipientName,
        string courseTitle,
        CancellationToken cancellationToken);

    Task SendPaymentConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string packageName,
        decimal amount,
        string transactionReference,
        CancellationToken cancellationToken);
}
