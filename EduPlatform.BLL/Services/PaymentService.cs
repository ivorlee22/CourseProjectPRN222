using EduPlatform.BLL.DTOs.Payments;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace EduPlatform.BLL.Services;

public sealed class PaymentService(
    IPaymentRepository paymentRepository,
    IPackageRepository packageRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    IVNPayService vnPayService,
    IEmailService emailService,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentUrlResponse> CreatePaymentAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Method != PaymentMethod.VNPay)
        {
            throw new NotSupportedException("EduPlatform currently supports VNPay payments only.");
        }

        var package = await packageRepository.GetByIdAsync(command.PackageId, cancellationToken);
        if (package == null || !package.IsActive)
        {
            throw new ArgumentException("Package not found or inactive.");
        }

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        if (command.Method != PaymentMethod.VNPay)
        {
            throw new ArgumentException(
                "Phương thức thanh toán không được hỗ trợ. Vui lòng chọn VNPay.");
        }

        var internalReference = $"PAY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";

        var payment = new Payment
        {
            UserId = user.Id,
            PackageId = package.Id,
            Amount = package.Price,
            Method = command.Method,
            Status = PaymentStatus.Pending,
            InternalReference = internalReference
        };

        paymentRepository.Add(payment);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        var paymentUrl = command.Method == PaymentMethod.VNPay
            ? vnPayService.CreatePaymentUrl(payment, command.ClientIpAddress)
            : throw new NotSupportedException(
                $"Payment method {command.Method} is not supported.");

        return new PaymentUrlResponse(paymentUrl);
    }

    public async Task<bool> ProcessCallbackAsync(PaymentCallbackCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Method != PaymentMethod.VNPay)
        {
            logger.LogWarning(
                "Unsupported payment method callback {Method}",
                command.Method);
            return false;
        }

        var isSignatureValid = vnPayService.VerifySignature(command.QueryData);
        if (!isSignatureValid)
        {
            logger.LogWarning("Invalid VNPay payment signature.");
            return false;
        }

        var reference = command.QueryData.TryGetValue("vnp_TxnRef", out var vnpRef) ? vnpRef : string.Empty;
        var gatewayTxnId = command.QueryData.TryGetValue("vnp_TransactionNo", out var vnpNo) ? vnpNo : string.Empty;
        var responseCode = command.QueryData.TryGetValue("vnp_ResponseCode", out var vnpRc) ? vnpRc : string.Empty;

        if (string.IsNullOrEmpty(reference))
        {
            return false;
        }

        var payment = await paymentRepository.GetByInternalReferenceAsync(reference, cancellationToken);
        if (payment == null)
        {
            logger.LogWarning("Payment not found for reference {Reference}", reference);
            return false;
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return true;
        }

        payment.GatewayTransactionId = gatewayTxnId;
        payment.GatewayResponseCode = responseCode;
        payment.ProcessedAtUtc = DateTimeOffset.UtcNow;
        payment.RawResponseJson = System.Text.Json.JsonSerializer.Serialize(command.QueryData);

        var isSuccess = responseCode == "00";

        if (isSuccess)
        {
            payment.Status = PaymentStatus.Succeeded;

            var existingSubscription = await subscriptionRepository.GetActiveSubscriptionAsync(payment.UserId, cancellationToken);
            if (existingSubscription != null)
            {
                existingSubscription.Status = SubscriptionStatus.Cancelled;
                existingSubscription.CancelledAtUtc = DateTimeOffset.UtcNow;
                subscriptionRepository.Update(existingSubscription);
            }

            var package = payment.Package;
            var subscription = new Subscription
            {
                UserId = payment.UserId,
                PackageId = package.Id,
                Status = SubscriptionStatus.Active,
                StartsAtUtc = DateTimeOffset.UtcNow,
                EndsAtUtc = DateTimeOffset.UtcNow.AddDays(package.DurationDays)
            };

            await subscriptionRepository.AddAsync(subscription, cancellationToken);

            payment.Subscription = subscription;

            try
            {
                var user = payment.User;
                if (user != null)
                {
                    await emailService.SendPaymentConfirmationAsync(
                        user.Email,
                        user.FullName,
                        package.Name,
                        package.Price,
                        payment.InternalReference,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send payment confirmation email for payment {PaymentId}", payment.Id);
            }
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
        }

        paymentRepository.Update(payment);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        return isSuccess;
    }

    public async Task<List<PaymentSummaryDto>> GetUserPaymentsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var payments = await paymentRepository.GetByUserIdAsync(userId, cancellationToken);

        return payments.Select(p => new PaymentSummaryDto(
            p.Id,
            p.PackageId,
            p.Package.Name,
            p.Amount,
            p.Method,
            p.Status,
            p.InternalReference,
            p.CreatedAtUtc,
            p.ProcessedAtUtc
        )).ToList();
    }

    public async Task<PaymentDetailDto?> GetPaymentDetailAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(id, cancellationToken);
        if (payment == null || payment.UserId != userId)
        {
            return null;
        }

        var status = payment.Status;
        if (status == PaymentStatus.Pending && DateTimeOffset.UtcNow - payment.CreatedAtUtc > TimeSpan.FromMinutes(15))
        {
            status = PaymentStatus.Failed;
        }

        return new PaymentDetailDto(
            payment.Id,
            payment.UserId,
            payment.PackageId,
            payment.Package.Name,
            payment.Amount,
            payment.Method,
            status,
            payment.InternalReference,
            payment.GatewayTransactionId,
            payment.CreatedAtUtc,
            payment.ProcessedAtUtc
        );
    }
}