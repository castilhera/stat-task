using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Stat.Core.Extensions;
using System.Text.RegularExpressions;

namespace Stat.Core.BlobStorage.Aws;

public class AwsS3BlobStorage
    : BlobStorageBase, IDisposable
{
    private bool _disposed;

    private readonly AwsS3BlobStorageOptions _options;
    private readonly AmazonS3Client _client;

    public AwsS3BlobStorage(IOptions<AwsS3BlobStorageOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrEmpty(_options.ServiceUrl))
        {
            var region = RegionEndpoint.GetBySystemName(_options.Region);

            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, region);
        }
        else
        {
            var config = new AmazonS3Config()
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                UseHttp = true,
                LogResponse = true,
                LogMetrics = true
            };

            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }
    }

    private static string CreateKey(string fileName, string path = "")
    {
        return Path.Join(path, fileName).Replace("\\", "/");
    }

    private static IEnumerable<string> FilterFilesByPattern(IEnumerable<string> files, string searchPattern)
    {
        string regexPattern = $"^{Regex.Escape(searchPattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

        return files.Where(f => regex.IsMatch(f));
    }

    public override async Task SaveAsync(string containerName, string fileName, Stream blob, string path = "", bool overrideExisting = true, CancellationToken cancellationToken = default)
    {
        var stream = await blob.CopyToMemoryStreamAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = containerName,
            Key = CreateKey(fileName, path),
            InputStream = stream
        };

        if (!overrideExisting)
        {
            request.IfNoneMatch = "*";
        }

        try
        {
            _ = await _client.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex)
            when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
        }
    }

    public override async Task<List<string>> ListAsync(string containerName, string path = "", string searchPattern = "", CancellationToken cancellationToken = default)
    {
        string continuationToken = string.Empty;

        var request = new ListObjectsV2Request
        {
            BucketName = containerName,
            Prefix = path
        };

        var response = await _client.ListObjectsV2Async(request, cancellationToken);

        var keys = response.S3Objects.Select(x => x.Key);

        if (!string.IsNullOrWhiteSpace(searchPattern))
        {
            keys = FilterFilesByPattern(keys, searchPattern);
        }

        return [.. keys];
    }

    public override async Task<Stream?> GetOrNullAsync(string containerName, string fileName, string path = "", CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = containerName,
            Key = CreateKey(fileName, path)
        };

        try
        {
            var response = await _client.GetObjectAsync(request, cancellationToken);

            var stream = await response.ResponseStream.CopyToMemoryStreamAsync(cancellationToken);

            return stream;
        }
        catch (AmazonS3Exception ex)
            when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _client.Dispose();
        GC.SuppressFinalize(this);
        _disposed = true;
    }
}