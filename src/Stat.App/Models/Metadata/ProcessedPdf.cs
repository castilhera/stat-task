using System.Text.Json.Serialization;

namespace Stat.App.Models.Metadata;

[method: JsonConstructor]
public class ProcessedPdf(string pdfFilename)
{
    [JsonPropertyOrder(0)]
    [JsonPropertyName("pdf")]
    public string PdfFilename { get; private set; } = pdfFilename;

    [JsonPropertyOrder(1)]
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyOrder(2)]
    [JsonPropertyName("extrated_on")]
    public DateTime ExtractedOn { get; } = DateTime.Now;
}
