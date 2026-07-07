using EduPlatform.BLL.DTOs.Payments;

namespace EduPlatform.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentUrlResponse> CreatePaymentAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default);
    Task<bool> ProcessCallbackAsync(PaymentCallbackCommand command, CancellationToken cancellationToken = default);
    Task<List<PaymentSummaryDto>> GetUserPaymentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PaymentDetailDto?> GetPaymentDetailAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
