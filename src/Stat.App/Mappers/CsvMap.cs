using CsvHelper.Configuration;
using Stat.App.Models.Csv;

namespace Stat.App.Mappers;

public class CsvMap : ClassMap<CsvItem>
{
    private const string PONumberColumnName = "PO Number";
    private const string AttachmentListColumnName = "Attachment List";

    public CsvMap()
    {
        Map(m => m.PONumber)
            .Name(PONumberColumnName);

        Map(m => m.Attachments)
            .Name(AttachmentListColumnName)
            .Convert(o => ParseAttachmentsToList(o.Row.GetField(AttachmentListColumnName)));
    }

    private static List<CsvItemAttachment> ParseAttachmentsToList(string? values)
    {
        return values?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(attachment => new CsvItemAttachment
            {
                Path = Path.GetDirectoryName(attachment) ?? string.Empty,
                Filename = Path.GetFileName(attachment),
            })
            .ToList() ?? [];
    }
}
