namespace Stat.Core.BlobStorage.Aws;

public class AwsS3BlobStorageOptions
{
    /// <summary>
    /// AWS Access Key ID
    /// </summary>
    public required string AccessKey { get; set; }

    /// <summary>
    /// AWS Secret Access Key
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// AWS Region name
    /// </summary>
    public required string Region { get; set; }
    
    /// <summary>
    /// Set the ServiceUrl for testing locally
    /// </summary>
    public string? ServiceUrl { get; set; }
}
