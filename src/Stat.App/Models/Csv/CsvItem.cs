namespace Stat.App.Models.Csv;

public class CsvItem
{
    public string PONumber { get; set; } = string.Empty;
    public List<CsvItemAttachment> Attachments { get; set; } = [];
}
