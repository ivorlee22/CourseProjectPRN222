using EduPlatform.BLL.DTOs.Payments;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Logging;
using BllPaymentMethod = EduPlatform.BLL.Enums.PaymentMethod;
using BllPaymentStatus = EduPlatform.BLL.Enums.PaymentStatus;
using DalPaymentMethod = EduPlatform.DAL.Entities.PaymentMethod;
using DalPaymentStatus = EduPlatform.DAL.Entities.PaymentStatus;

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
    public async Task<PaymentUrlResponse> CreatePaymentAsync(
        CreatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Method != BllPaymentMethod.VNPay)
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

        if (user.Role != UserRole.Student)
        {
            throw new ArgumentException("Only students can buy packages.");
        }

        var internalReference = $"PAY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";

        var payment = new Payment
        {
            UserId = user.Id,
            PackageId = package.Id,
            Amount = package.Price,
            Method = MapPaymentMethod(command.Method),
            Status = DalPaymentStatus.Pending,
            InternalReference = internalReference
        };

        paymentRepository.Add(payment);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        var paymentUrl = vnPayService.CreatePaymentUrl(payment, command.ClientIpAddress);
        return new PaymentUrlResponse(paymentUrl);
    }

    public async Task<bool> ProcessCallbackAsync(
        PaymentCallbackCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Method != BllPaymentMethod.VNPay)
        {
            logger.LogWarning("Unsupported payment callback method {Method}", command.Method);
            return false;
        }

        if (!vnPayService.VerifySignature(command.QueryData))
        {
            logger.LogWarning("Invalid payment signature for method {Method}", command.Method);
            return false;
        }

        var reference = command.QueryData.TryGetValue("vnp_TxnRef", out var vnpRef)
            ? vnpRef
            : string.Empty;

        var gatewayTxnId = command.QueryData.TryGetValue("vnp_TransactionNo", out var vnpNo)
            && vnpNo != "0"
            && !string.IsNullOrWhiteSpace(vnpNo)
                ? vnpNo
                : null;

        var responseCode = command.QueryData.TryGetValue("vnp_ResponseCode", out var vnpRc)
            ? vnpRc
            : string.Empty;

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

        if (payment.Status != DalPaymentStatus.Pending)
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
            payment.Status = DalPaymentStatus.Succeeded;
            await ApplySubscriptionChangeAsync(payment, cancellationToken);
        }
        else
        {
            payment.Status = DalPaymentStatus.Failed;
        }

        paymentRepository.Update(payment);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        if (isSuccess)
        {
            await SendConfirmationEmailAsync(payment, cancellationToken);
        }

        return isSuccess;
    }

    public async Task<List<PaymentSummaryDto>> GetUserPaymentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var payments = await paymentRepository.GetByUserIdAsync(userId, cancellationToken);

        return payments.Select(p =>
        {
            var status = p.Status;
            if (status == DalPaymentStatus.Pending && DateTimeOffset.UtcNow - p.CreatedAtUtc > TimeSpan.FromMinutes(15))
            {
                status = DalPaymentStatus.Failed;
            }

            return new PaymentSummaryDto(
                p.Id,
                p.PackageId,
                p.Package.Name,
                p.Amount,
                MapPaymentMethod(p.Method),
                MapPaymentStatus(status),
                p.InternalReference,
                p.CreatedAtUtc,
                p.ProcessedAtUtc);
        }).ToList();
    }

    public async Task<PaymentDetailDto?> GetPaymentDetailAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(id, cancellationToken);
        if (payment == null || payment.UserId != userId)
        {
            return null;
        }

        var status = payment.Status;
        if (status == DalPaymentStatus.Pending && DateTimeOffset.UtcNow - payment.CreatedAtUtc > TimeSpan.FromMinutes(15))
        {
            status = DalPaymentStatus.Failed;
        }

        return new PaymentDetailDto(
            payment.Id,
            payment.UserId,
            payment.PackageId,
            payment.Package.Name,
            payment.Amount,
            MapPaymentMethod(payment.Method),
            MapPaymentStatus(status),
            payment.InternalReference,
            payment.GatewayTransactionId,
            payment.CreatedAtUtc,
            payment.ProcessedAtUtc);
    }

    private async Task ApplySubscriptionChangeAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        var package = payment.Package;
        var now = DateTimeOffset.UtcNow;
        var existingSubscription = await subscriptionRepository.GetActiveSubscriptionAsync(
            payment.UserId,
            cancellationToken);

        Subscription subscription;
        if (existingSubscription is null)
        {
            subscription = CreateSubscription(
                payment.UserId,
                package,
                SubscriptionStatus.Active,
                now,
                now.AddDays(package.DurationDays));
        }
        else if (package.Price > GetSubscriptionPrice(existingSubscription))
        {
            existingSubscription.Status = SubscriptionStatus.Expired;
            existingSubscription.EndsAtUtc = now;
            subscriptionRepository.Update(existingSubscription);

            subscription = CreateSubscription(
                payment.UserId,
                package,
                SubscriptionStatus.Active,
                now,
                now.AddDays(package.DurationDays));
        }
        else
        {
            var startsAtUtc = existingSubscription.EndsAtUtc;
            subscription = CreateSubscription(
                payment.UserId,
                package,
                SubscriptionStatus.Active,
                startsAtUtc,
                startsAtUtc.AddDays(package.DurationDays));
        }

        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        payment.Subscription = subscription;
    }

    private async Task SendConfirmationEmailAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = payment.User;
            if (user != null)
            {
                await emailService.SendPaymentConfirmationAsync(
                    user.Email,
                    user.FullName,
                    payment.Package.Name,
                    payment.Package.Price,
                    payment.InternalReference,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send payment confirmation email for payment {PaymentId}", payment.Id);
        }
    }

    private static Subscription CreateSubscription(
        Guid userId,
        Package package,
        SubscriptionStatus status,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        return new Subscription
        {
            UserId = userId,
            PackageId = package.Id,
            Package = package,
            Status = status,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc
        };
    }

    private static decimal GetSubscriptionPrice(Subscription subscription)
    {
        return subscription.Package.Price;
    }

    private static DalPaymentMethod MapPaymentMethod(BllPaymentMethod method) => method switch
    {
        BllPaymentMethod.VNPay => DalPaymentMethod.VNPay,
        BllPaymentMethod.MoMo => DalPaymentMethod.MoMo,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
    };

    private static BllPaymentMethod MapPaymentMethod(DalPaymentMethod method) => method switch
    {
        DalPaymentMethod.VNPay => BllPaymentMethod.VNPay,
        DalPaymentMethod.MoMo => BllPaymentMethod.MoMo,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
    };

    private static BllPaymentStatus MapPaymentStatus(DalPaymentStatus status) => status switch
    {
        DalPaymentStatus.Pending => BllPaymentStatus.Pending,
        DalPaymentStatus.Succeeded => BllPaymentStatus.Succeeded,
        DalPaymentStatus.Failed => BllPaymentStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
