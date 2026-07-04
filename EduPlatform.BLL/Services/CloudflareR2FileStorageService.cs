using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using EduPlatform.BLL.Interfaces;

namespace EduPlatform.BLL.Services
{
    public sealed class CloudflareR2FileStorageService : IFileStorageService, IDisposable
    {
        private readonly AmazonS3Client _s3Client;
        private readonly HttpClient _httpClient;
        private readonly string _bucketName;
        private readonly string? _publicUrl;
        private readonly int _downloadExpiryMinutes;
        private readonly string _endpoint;
        private readonly string _accessKeyId;
        private readonly string _secretAccessKey;
        private readonly string _region;

        public CloudflareR2FileStorageService(IConfiguration configuration, HttpClient httpClient)
        {
            _accessKeyId = FirstNonEmpty(
                configuration["CloudStorage:R2:AccessKeyId"],
                configuration["R2_ACCESS_KEY"]) ?? string.Empty;

            var secretAccessKey = FirstNonEmpty(
                configuration["CloudStorage:R2:SecretAccessKey"],
                configuration["R2_SECRET_KEY"]);
            _secretAccessKey = secretAccessKey ?? string.Empty;

            _endpoint = FirstNonEmpty(
                configuration["CloudStorage:R2:Endpoint"],
                configuration["R2_ENDPOINT"]) ?? string.Empty;

            _bucketName = FirstNonEmpty(
                configuration["CloudStorage:R2:BucketName"],
                configuration["R2_BUCKET"]) ?? string.Empty;

            _publicUrl = FirstNonEmpty(
                configuration["CloudStorage:R2:PublicUrl"],
                configuration["R2_PUBLIC_URL"]);

            _region = FirstNonEmpty(
                configuration["CloudStorage:R2:Region"],
                configuration["R2_REGION"]) ?? "auto";

            if (string.IsNullOrWhiteSpace(_endpoint))
            {
                throw new InvalidOperationException("Thiếu cấu hình CloudStorage:R2:Endpoint");
            }

            if (string.IsNullOrWhiteSpace(_accessKeyId))
            {
                throw new InvalidOperationException("Thiếu cấu hình CloudStorage:R2:AccessKeyId");
            }

            if (string.IsNullOrWhiteSpace(_secretAccessKey))
            {
                throw new InvalidOperationException("Thiếu cấu hình CloudStorage:R2:SecretAccessKey");
            }

            if (string.IsNullOrWhiteSpace(_bucketName))
            {
                throw new InvalidOperationException("Thiếu cấu hình CloudStorage:R2:BucketName");
            }

            _downloadExpiryMinutes = int.TryParse(
                configuration["CloudStorage:R2:DownloadExpiryMinutes"],
                out var expiryMinutes)
                ? expiryMinutes
                : 15;

            var s3Config = new AmazonS3Config
            {
                ServiceURL = _endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = _region
            };

            var credentials = new BasicAWSCredentials(_accessKeyId, secretAccessKey);
            _s3Client = new AmazonS3Client(credentials, s3Config);
            _httpClient = httpClient;
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            using var buffer = new MemoryStream();
            await fileStream.CopyToAsync(buffer);
            var payload = buffer.ToArray();
            buffer.Position = 0;

            var safeFileName = Path.GetFileName(fileName);
            var objectKey = $"documents/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{safeFileName}";

            await UploadObjectAsync(objectKey, payload, contentType);

            return objectKey;
        }

        private async Task UploadObjectAsync(string objectKey, byte[] payload, string contentType)
        {
            var now = DateTime.UtcNow;
            var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            var endpoint = _endpoint.TrimEnd('/');
            var host = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? endpoint.Substring("http://".Length)
                : endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? endpoint.Substring("https://".Length)
                    : endpoint;

            var safeHost = host.TrimEnd('/');
            var canonicalUri = $"/{_bucketName}/{Uri.EscapeDataString(objectKey).Replace("%2F", "/")}";
            var contentLength = payload.Length.ToString(CultureInfo.InvariantCulture);

            const string httpMethod = "PUT";
            const string service = "s3";
            const string signedHeaders = "content-length;host;x-amz-content-sha256;x-amz-date";
            const string payloadHash = "UNSIGNED-PAYLOAD";

            var canonicalRequest = string.Join('\n',
                httpMethod,
                canonicalUri,
                "",
                $"content-length:{contentLength}\n" +
                $"host:{safeHost}\n" +
                $"x-amz-content-sha256:{payloadHash}\n" +
                $"x-amz-date:{amzDate}\n",
                signedHeaders,
                payloadHash);

            var credentialScope = $"{dateStamp}/{_region}/{service}/aws4_request";
            var stringToSign = string.Join('\n',
                "AWS4-HMAC-SHA256",
                amzDate,
                credentialScope,
                Sha256Hex(canonicalRequest));

            var signingKey = BuildSigningKey(_secretAccessKey, dateStamp, _region, service);
            var signature = HmacSha256Hex(signingKey, stringToSign);

            var authorization = $"AWS4-HMAC-SHA256 Credential={_accessKeyId}/{credentialScope}, " +
                                $"SignedHeaders={signedHeaders}, Signature={signature}";

            var scheme = _endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "http" : "https";
            var url = $"{scheme}://{safeHost}{canonicalUri}";

            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
            request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
            request.Headers.TryAddWithoutValidation("Host", safeHost);
            request.Content = new ByteArrayContent(payload);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            request.Content.Headers.ContentLength = payload.Length;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Cloudflare R2 PUT failed. Status={(int)response.StatusCode}, Body={body}");
            }
        }

        private static byte[] BuildSigningKey(string secretAccessKey, string dateStamp, string region, string service)
        {
            const string aws4Request = "aws4_request";
            var kSecret = Encoding.UTF8.GetBytes("AWS4" + secretAccessKey);
            var kDate = HmacSha256(kSecret, dateStamp);
            var kRegion = HmacSha256(kDate, region);
            var kService = HmacSha256(kRegion, service);
            return HmacSha256(kService, aws4Request);
        }

        private static byte[] HmacSha256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string HmacSha256Hex(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return ToHex(bytes);
        }

        private static string Sha256Hex(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return ToHex(SHA256.HashData(bytes));
        }

        private static string ToHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            foreach (var b in data)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        public Task<string> GetDownloadUrlAsync(string storedPath, string fileName, string contentType)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                throw new InvalidOperationException("Đường dẫn cloud của tài liệu không hợp lệ.");
            }

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = storedPath,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(_downloadExpiryMinutes),
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = $"attachment; filename=\"{Path.GetFileName(fileName)}\"",
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
                }
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(presignedUrl);
        }

        public async Task DeleteAsync(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return;
            }

            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = storedPath
            });
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }

        public void Dispose()
        {
            _s3Client?.Dispose();
        }
    }
}
