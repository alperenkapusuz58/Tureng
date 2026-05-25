using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;

namespace Kelimebull.TtsWorker.Services;

public interface IR2AudioStorage
{
    Task<string> UploadAsync(string storageKey, string contentType, byte[] bytes, CancellationToken cancellationToken = default);
}

public sealed class R2AudioStorage : IR2AudioStorage
{
    private readonly TtsOptions _options;

    public R2AudioStorage(IOptions<TtsOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadAsync(string storageKey, string contentType, byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.R2.AccountId) ||
            string.IsNullOrWhiteSpace(_options.R2.BucketName) ||
            string.IsNullOrWhiteSpace(ResolveAccessKeyId()) ||
            string.IsNullOrWhiteSpace(ResolveSecretAccessKey()))
        {
            throw new InvalidOperationException("Cloudflare R2 configuration is incomplete.");
        }

        using var client = CreateClient();
        await using var stream = new MemoryStream(bytes);
        var request = new PutObjectRequest
        {
            BucketName = _options.R2.BucketName,
            Key = storageKey,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        };

        await client.PutObjectAsync(request, cancellationToken);
        return BuildPublicUrl(storageKey);
    }

    private AmazonS3Client CreateClient()
    {
        var credentials = new BasicAWSCredentials(ResolveAccessKeyId(), ResolveSecretAccessKey());
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_options.R2.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        return new AmazonS3Client(credentials, config);
    }

    private string BuildPublicUrl(string storageKey)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(_options.CdnBaseUrl)
            ? _options.CdnBaseUrl
            : _options.R2.PublicBaseUrl;

        return $"{baseUrl.TrimEnd('/')}/{storageKey.TrimStart('/')}";
    }

    private string ResolveAccessKeyId()
    {
        return Environment.GetEnvironmentVariable("TTS_R2_ACCESS_KEY_ID") ?? _options.R2.AccessKeyId;
    }

    private string ResolveSecretAccessKey()
    {
        return Environment.GetEnvironmentVariable("TTS_R2_SECRET_ACCESS_KEY") ?? _options.R2.SecretAccessKey;
    }
}
