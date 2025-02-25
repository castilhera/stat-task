namespace Stat.Core.Extensions;

public static class StreamExtensions
{
    public static async Task<Stream?> CopyToMemoryStreamAsync(this Stream? stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}
