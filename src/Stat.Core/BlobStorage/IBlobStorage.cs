namespace Stat.Core.BlobStorage;

public interface IBlobStorage
{
    Task<Stream?> GetOrNullAsync(string containerName, string fileName, string path = "", CancellationToken cancellationToken = default);

    Task<List<string>> ListAsync(string containerName, string path = "", string searchPattern = "", CancellationToken cancellationToken = default);

    Task SaveAsync(string containerName, string fileName, Stream blob, string path = "", bool overrideExisting = true, CancellationToken cancellationToken = default);
}