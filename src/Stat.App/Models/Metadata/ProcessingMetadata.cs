namespace Stat.App.Models.Metadata;

public class ProcessingMetadata
    : List<ProcessedZip>
{
    public const string Filename = $"{nameof(ProcessingMetadata)}.json";
}
