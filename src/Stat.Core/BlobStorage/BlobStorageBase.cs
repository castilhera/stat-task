namespace Stat.Core.BlobStorage;

public abstract class BlobStorageBase : IBlobStorage
{
    public abstract Task<Stream?> GetOrNullAsync(string containerName, string fileName, string path = "", CancellationToken cancellationToken = default);

    public abstract Task<List<string>> ListAsync(string containerName, string path = "", string searchPattern = "", CancellationToken cancellationToken = default);

    public abstract Task SaveAsync(string containerName, string fileName, Stream blob, string path = "", bool overrideExisting = true, CancellationToken cancellationToken = default);
}