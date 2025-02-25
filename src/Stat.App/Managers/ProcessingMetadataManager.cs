using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stat.App.Configs;
using Stat.App.Models.Metadata;
using Stat.App.Services;
using Stat.Core.BlobStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stat.App.Managers;

public class ProcessingMetadataManager
(
    ILogger<ProcessingMetadataManager> logger,
    IBlobStorage blobStorage,
    IOptions<AppConfig> options
)
{
    private readonly ILogger<ProcessingMetadataManager> _logger = logger;
    private readonly IBlobStorage _blobStorage = blobStorage;
    private readonly AppConfig _config = options.Value;

    public async Task<ProcessingMetadata> LoadAsync(CancellationToken cancellationToken = default)
    {
        using var file = await _blobStorage.GetOrNullAsync(_config.Container, ProcessingMetadata.Filename, cancellationToken: cancellationToken);

        if (file == null)
            return [];

        return (await JsonSerializer.DeserializeAsync<ProcessingMetadata>(file, cancellationToken: cancellationToken))!;
    }

    public async Task SaveAsync(ProcessingMetadata processingMetadata, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(processingMetadata);

        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        await _blobStorage.SaveAsync
        (
            containerName: _config.Container,
            ProcessingMetadata.Filename,
            blob: jsonStream,
            overrideExisting: true,
            cancellationToken: cancellationToken
        );
    }
}
