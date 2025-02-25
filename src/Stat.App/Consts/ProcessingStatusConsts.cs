namespace Stat.App.Consts;

public static class ProcessingStatusConsts
{
    public const string Error = "Error";

    public const string UnableToReadZipError = $"{Error}:UnableToReadZip";
    public const string CsvNotFoundError = $"{Error}:CsvNotFound";
    public const string PdfNotFoundError = $"{Error}:PdfNotFound";

    public const string Partial = "Partial";

    public const string UnknownPONumber = $"{Partial}:UnknownPONumber";

    public const string Success = "Success";
}
