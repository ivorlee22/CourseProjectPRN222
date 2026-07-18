using System.Security.Cryptography;
using System.Text;
using System.Web; // Đảm bảo có cái này để dùng HttpUtility
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using EduPlatform.DAL.Entities;
using Microsoft.Extensions.Options;

namespace EduPlatform.BLL.Services;

public sealed class VNPayService(IOptions<VNPayOptions> options) : IVNPayService
{
    private readonly VNPayOptions _options = options.Value;

    public string CreatePaymentUrl(Payment payment, string clientIpAddress)
    {
        if (clientIpAddress == "::1" || string.IsNullOrEmpty(clientIpAddress))
            clientIpAddress = "127.0.0.1";

        var vnpayData = new SortedList<string, string>(new VnPayCompare());
        vnpayData.Add("vnp_Version", "2.1.0");
        vnpayData.Add("vnp_Command", "pay");
        vnpayData.Add("vnp_TmnCode", _options.TmnCode?.Trim() ?? string.Empty);
        vnpayData.Add("vnp_Amount", ((long)(payment.Amount * 100)).ToString(System.Globalization.CultureInfo.InvariantCulture));
        vnpayData.Add("vnp_CreateDate", payment.CreatedAtUtc.AddHours(7).ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture));
        vnpayData.Add("vnp_CurrCode", "VND");
        vnpayData.Add("vnp_ExpireDate", payment.CreatedAtUtc.AddHours(7).AddMinutes(15).ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture));
        vnpayData.Add("vnp_IpAddr", clientIpAddress);
        vnpayData.Add("vnp_Locale", "vn");
        vnpayData.Add("vnp_OrderInfo", $"Payment_{payment.InternalReference}");
        vnpayData.Add("vnp_OrderType", "other");
        vnpayData.Add("vnp_ReturnUrl", _options.ReturnUrl?.Trim() ?? string.Empty);
        vnpayData.Add("vnp_TxnRef", payment.InternalReference);

        var data = new StringBuilder();
        foreach (var kv in vnpayData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                data.Append(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&");
            }
        }
        string queryString = data.ToString().TrimEnd('&');

        var vnpSecureHash = ComputeHmacSha512(_options.HashSecret?.Trim() ?? string.Empty, queryString);
        return $"{_options.PaymentUrl}?{queryString}&vnp_SecureHash={vnpSecureHash}";
    }

    public bool VerifySignature(IDictionary<string, string> queryData)
    {
        var vnpSecureHash = queryData.TryGetValue("vnp_SecureHash", out var hash) ? hash : string.Empty;
        if (string.IsNullOrEmpty(vnpSecureHash)) return false;

        var vnpayData = new SortedList<string, string>(new VnPayCompare());
        foreach (var kv in queryData)
        {
            if (kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)
                && kv.Key != "vnp_SecureHash"
                && kv.Key != "vnp_SecureHashType")
            {
                if (!string.IsNullOrEmpty(kv.Value))
                    vnpayData.Add(kv.Key, kv.Value);
            }
        }

        var data = new StringBuilder();
        foreach (var kv in vnpayData)
        {
            data.Append(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&");
        }
        string signData = data.ToString().TrimEnd('&');

        var computedHash = ComputeHmacSha512(_options.HashSecret?.Trim() ?? string.Empty, signData);
        return computedHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHmacSha512(string key, string data)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        byte[] hashValue = hmac.ComputeHash(inputBytes);

        return Convert.ToHexString(hashValue).ToLowerInvariant();
    }

    private sealed class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = System.Globalization.CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, System.Globalization.CompareOptions.Ordinal);
        }
    }
}