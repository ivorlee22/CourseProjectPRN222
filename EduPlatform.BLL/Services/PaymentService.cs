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
    IMoMoService moMoService,
    IEmailService emailService,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentUrlResponse> CreatePaymentAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default)
    {
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

        string paymentUrl;
        if (command.Method == PaymentMethod.VNPay)
        {
            paymentUrl = vnPayService.CreatePaymentUrl(payment, command.ClientIpAddress);
        }
        else if (command.Method == PaymentMethod.MoMo)
        {
            paymentUrl = await moMoService.CreatePaymentUrlAsync(payment);
        }
        else
        {
            throw new NotSupportedException($"Payment method {command.Method} is not supported.");
        }

        return new PaymentUrlResponse(paymentUrl);
    }

    public async Task<bool> ProcessCallbackAsync(PaymentCallbackCommand command, CancellationToken cancellationToken = default)
    {
        var isSignatureValid = command.Method switch
        {
            PaymentMethod.VNPay => vnPayService.VerifySignature(command.QueryData),
            PaymentMethod.MoMo => moMoService.VerifySignature(command.QueryData),
            _ => false
        };

        if (!isSignatureValid)
        {
            logger.LogWarning("Invalid payment signature for method {Method}", command.Method);
            return false;
        }

        var reference = command.Method == PaymentMethod.VNPay
            ? (command.QueryData.TryGetValue("vnp_TxnRef", out var vnpRef) ? vnpRef : string.Empty)
            : (command.QueryData.TryGetValue("orderId", out var orderId) ? orderId : string.Empty);
            
        var gatewayTxnId = command.Method == PaymentMethod.VNPay
            ? (command.QueryData.TryGetValue("vnp_TransactionNo", out var vnpNo) ? vnpNo : string.Empty)
            : (command.QueryData.TryGetValue("transId", out var transId) ? transId : string.Empty);
            
        var responseCode = command.Method == PaymentMethod.VNPay
            ? (command.QueryData.TryGetValue("vnp_ResponseCode", out var vnpRc) ? vnpRc : string.Empty)
            : (command.QueryData.TryGetValue("resultCode", out var momoRc) ? momoRc : string.Empty);

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

        // Idempotency check
        if (payment.Status != PaymentStatus.Pending)
        {
            return true;
        }

        payment.GatewayTransactionId = gatewayTxnId;
        payment.GatewayResponseCode = responseCode;
        payment.ProcessedAtUtc = DateTimeOffset.UtcNow;
        payment.RawResponseJson = System.Text.Json.JsonSerializer.Serialize(command.QueryData);

        var isSuccess = command.Method == PaymentMethod.VNPay 
            ? responseCode == "00" 
            : responseCode == "0";

        if (isSuccess)
        {
            payment.Status = PaymentStatus.Succeeded;
            
            // Cancel existing active subscription if any and create new
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
            
            // Assign subscription to payment
            payment.Subscription = subscription;

            // Send confirmation email asynchronously (fire and forget or just await here depending on design)
            // Assuming emailService throws on fail, we might want to catch it to not fail the transaction
            try
            {
                var user = payment.User; // EF should load this or we query
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

        return new PaymentDetailDto(
            payment.Id,
            payment.UserId,
            payment.PackageId,
            payment.Package.Name,
            payment.Amount,
            payment.Method,
            payment.Status,
            payment.InternalReference,
            payment.GatewayTransactionId,
            payment.CreatedAtUtc,
            payment.ProcessedAtUtc
        );
    }
}
