using System.Text.Json.Serialization;

namespace Stat.App.Models.Metadata;

[method: JsonConstructor]
public class ProcessedZip(string zipFilename, List<ProcessedPdf>? pdfFiles = null)
{
    [JsonPropertyOrder(0)]
    [JsonPropertyName("zip")]
    public string ZipFilename { get; private set; } = zipFilename;

    [JsonPropertyOrder(1)]
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyOrder(2)]
    [JsonPropertyName("pdfs")]
    public List<ProcessedPdf> PdfFiles { get; } = pdfFiles ?? [];
}
