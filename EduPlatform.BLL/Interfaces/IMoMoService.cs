using EduPlatform.DAL.Entities;

namespace EduPlatform.BLL.Interfaces;

public interface IMoMoService
{
    Task<string> CreatePaymentUrlAsync(Payment payment);
    bool VerifySignature(IDictionary<string, string> queryData);
}
