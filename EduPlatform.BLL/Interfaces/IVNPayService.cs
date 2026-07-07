using EduPlatform.DAL.Entities;

namespace EduPlatform.BLL.Interfaces;

public interface IVNPayService
{
    string CreatePaymentUrl(Payment payment, string clientIpAddress);
    bool VerifySignature(IDictionary<string, string> queryData);
}
