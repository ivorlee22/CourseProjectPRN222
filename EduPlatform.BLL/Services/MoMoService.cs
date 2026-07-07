using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using EduPlatform.DAL.Entities;
using Microsoft.Extensions.Options;

namespace EduPlatform.BLL.Services;

public sealed class MoMoService(IOptions<MoMoOptions> options, IHttpClientFactory httpClientFactory) : IMoMoService
{
    private readonly MoMoOptions _options = options.Value;

    public async Task<string> CreatePaymentUrlAsync(Payment payment)
    {
        var requestId = Guid.NewGuid().ToString();
        var orderId = payment.InternalReference;
        var amount = ((long)payment.Amount).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var orderInfo = $"Thanh toan don hang {orderId}";
        var returnUrl = _options.ReturnUrl ?? string.Empty;
        var ipnUrl = _options.IpnUrl ?? string.Empty;
        var partnerCode = _options.PartnerCode ?? string.Empty;
        var requestType = "captureWallet";
        var extraData = "";

        var rawSignature = $"accessKey={_options.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
        var signature = ComputeHmacSha256(_options.SecretKey ?? string.Empty, rawSignature);

        var requestData = new
        {
            partnerCode = partnerCode,
            partnerName = "Test",
            storeId = "MomoTestStore",
            requestId = requestId,
            amount = amount,
            orderId = orderId,
            orderInfo = orderInfo,
            redirectUrl = returnUrl,
            ipnUrl = ipnUrl,
            lang = "vi",
            extraData = extraData,
            requestType = requestType,
            signature = signature
        };

        var client = httpClientFactory.CreateClient();
        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_options.PaymentUrl, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseString);
        var root = jsonDocument.RootElement;

        if (root.TryGetProperty("payUrl", out var payUrlElement))
        {
            return payUrlElement.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("message", out var messageElement))
        {
            throw new InvalidOperationException($"MoMo API error: {messageElement.GetString()}");
        }

        throw new InvalidOperationException("Failed to get payUrl from MoMo API response.");
    }

    public bool VerifySignature(IDictionary<string, string> queryData)
    {
        if (!queryData.TryGetValue("signature", out var signature)) return false;

        queryData.TryGetValue("accessKey", out var accessKey); // sometimes it's missing in return URL, but usually MoMo uses partnerCode
        var partnerCode = queryData.TryGetValue("partnerCode", out var p1) ? p1 : string.Empty;
        var orderId = queryData.TryGetValue("orderId", out var p2) ? p2 : string.Empty;
        var requestId = queryData.TryGetValue("requestId", out var p3) ? p3 : string.Empty;
        var amount = queryData.TryGetValue("amount", out var p4) ? p4 : string.Empty;
        var orderInfo = queryData.TryGetValue("orderInfo", out var p5) ? p5 : string.Empty;
        var orderType = queryData.TryGetValue("orderType", out var p6) ? p6 : string.Empty;
        var transId = queryData.TryGetValue("transId", out var p7) ? p7 : string.Empty;
        var message = queryData.TryGetValue("message", out var p8) ? p8 : string.Empty;
        var localMessage = queryData.TryGetValue("localMessage", out var p9) ? p9 : string.Empty;
        var responseTime = queryData.TryGetValue("responseTime", out var p10) ? p10 : string.Empty;
        var errorCode = queryData.TryGetValue("errorCode", out var p11) ? p11 : string.Empty;
        var payType = queryData.TryGetValue("payType", out var p12) ? p12 : string.Empty;
        var extraData = queryData.TryGetValue("extraData", out var p13) ? p13 : string.Empty;

        var rawSignature = $"accessKey={_options.AccessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={errorCode}&transId={transId}";
        
        var computedSignature = ComputeHmacSha256(_options.SecretKey, rawSignature);

        return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHmacSha256(string key, string data)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(data))
            return string.Empty;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashValue).ToLower(System.Globalization.CultureInfo.InvariantCulture);
    }
}
