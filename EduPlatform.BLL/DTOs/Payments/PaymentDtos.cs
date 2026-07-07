using EduPlatform.DAL.Entities;

namespace EduPlatform.BLL.DTOs.Payments;

public record PaymentSummaryDto(
    Guid Id,
    Guid PackageId,
    string PackageName,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string InternalReference,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ProcessedAtUtc);

public record PaymentDetailDto(
    Guid Id,
    Guid UserId,
    Guid PackageId,
    string PackageName,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string InternalReference,
    string? GatewayTransactionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ProcessedAtUtc);

public record CreatePaymentCommand(
    Guid UserId,
    Guid PackageId,
    PaymentMethod Method,
    string ClientIpAddress);

public record PaymentUrlResponse(string Url);

public record PaymentCallbackCommand(
    PaymentMethod Method,
    IDictionary<string, string> QueryData);
