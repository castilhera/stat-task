namespace Stat.App.Configs;

public class AppConfig
{
    /// <summary>
    /// Container / Bucket name
    /// </summary>
    public required string Container { get; set; }

    public required string UnknownPOFolderName { get; set; }
}
