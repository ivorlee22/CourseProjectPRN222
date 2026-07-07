namespace EduPlatform.DAL.Repositories;

using EduPlatform.DAL.Entities;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByInternalReferenceAsync(string internalReference, CancellationToken cancellationToken = default);
    Task<Payment?> GetByGatewayTransactionIdAsync(PaymentMethod method, string gatewayTransactionId, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Payment payment);
    void Update(Payment payment);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
