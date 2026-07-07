namespace EduPlatform.DAL.Repositories;

using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .Include(p => p.Package)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByInternalReferenceAsync(string internalReference, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .Include(p => p.Package)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.InternalReference == internalReference, cancellationToken);
    }

    public async Task<Payment?> GetByGatewayTransactionIdAsync(PaymentMethod method, string gatewayTransactionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .FirstOrDefaultAsync(p => p.Method == method && p.GatewayTransactionId == gatewayTransactionId, cancellationToken);
    }

    public async Task<List<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .Include(p => p.Package)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Add(Payment payment)
    {
        dbContext.Payments.Add(payment);
    }

    public void Update(Payment payment)
    {
        dbContext.Payments.Update(payment);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
