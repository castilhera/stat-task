using Amazon.Runtime.Internal.Util;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stat.App.Configs;
using Stat.App.Consts;
using Stat.App.Managers;
using Stat.App.Mappers;
using Stat.App.Models.Csv;
using Stat.App.Models.Metadata;
using Stat.Core.BlobStorage;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace Stat.App.Services;

public class AppService
(
    ILogger<AppService> logger,
    IBlobStorage blobStorage,
    IOptions<AppConfig> options,
    ProcessingMetadataManager metadataManager
)
{
    private readonly ILogger<AppService> _logger = logger;
    private readonly IBlobStorage _blobStorage = blobStorage;
    private readonly AppConfig _config = options.Value;
    private readonly ProcessingMetadataManager _metadataManager = metadataManager;

    public async Task ProcessNewFilesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var processingMetadata = await _metadataManager.LoadAsync(cancellationToken);

            var processedZips = new HashSet<string>(processingMetadata.Select(x => x.ZipFilename));

            var zipFiles = await _blobStorage.ListAsync
            (
                containerName: _config.Container,
                searchPattern: "*.zip",
                cancellationToken: cancellationToken
            );

            foreach (var zipFilename in zipFiles)
            {
                if (!processedZips.Contains(zipFilename))
                {
                    var processedZip = await ProcessZip(zipFilename, cancellationToken);
                    processingMetadata.Add(processedZip);
                }
            }

            await _metadataManager.SaveAsync(processingMetadata, cancellationToken);
        }
        finally
        {
            stopwatch.Stop();

            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                           ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

            _logger.LogInformation("Total process time: {elapsedTime}.", elapsedTime);
        }
    }

    private static List<CsvItem> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);

        using var csv = new CsvReader
        (
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "~",
                TrimOptions = TrimOptions.Trim,
                HasHeaderRecord = true,
                IgnoreBlankLines = true
            }
        );

        csv.Context.RegisterClassMap<CsvMap>();

        return [.. csv.GetRecords<CsvItem>()];
    }

    private static List<CsvItem>? GetCsvItems(ZipArchive archive)
    {
        var csvFile = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".csv"));

        if (csvFile == null)
        {
            return null;
        }

        using var csvStream = csvFile.Open();

        var csvItems = ParseCsv(csvStream);

        return csvItems;
    }

    private async Task<ProcessedPdf> ProcessPdf(string zipFilename, ZipArchive archive, string pdfFilename, string poNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ZIP {zipFilename} | Start processing PDF {pdfFilename}.", zipFilename, pdfFilename);

        var processedPdf = new ProcessedPdf(pdfFilename);

        var pdfFile = archive.Entries.FirstOrDefault(e => e.FullName.Equals(pdfFilename, StringComparison.InvariantCultureIgnoreCase));

        if (pdfFile == null)
        {
            _logger.LogWarning("ZIP {zipFilename} | PDF {pdfFilename} not found.", pdfFilename, zipFilename);
            processedPdf.Status = ProcessingStatusConsts.PdfNotFoundError;
            return processedPdf;
        }

        if (string.IsNullOrWhiteSpace(poNumber))
        {
            poNumber = _config.UnknownPOFolderName;
        }

        try
        {
            using var pdfStream = pdfFile.Open();

            await _blobStorage.SaveAsync
            (
                containerName: _config.Container,
                fileName: pdfFilename,
                blob: pdfStream,
                path: $"by-po/{poNumber}/",
                overrideExisting: false,
                cancellationToken: cancellationToken
            );

            processedPdf.Status = (poNumber != _config.UnknownPOFolderName)
                ? ProcessingStatusConsts.Success : ProcessingStatusConsts.UnknownPONumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZIP {zipFilename} | Error processing PDF {pdfFilename}.", zipFilename, zipFilename);
            processedPdf.Status = ProcessingStatusConsts.Error;
        }

        _logger.LogInformation("ZIP {zipFilename} | End processing PDF {pdfFilename}.", zipFilename, pdfFilename);

        return processedPdf;
    }

    private async Task<ProcessedZip> ProcessZip(string zipFilename, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Start processing ZIP {zipFilename}", zipFilename);

        var processedZip = new ProcessedZip(zipFilename);

        using var zipFileStream = await _blobStorage.GetOrNullAsync
        (
            containerName: _config.Container,
            fileName: zipFilename,
            cancellationToken: cancellationToken
        );

        if (zipFilename == null)
        {
            _logger.LogWarning("ZIP {zipFilename} | Unable to read ZIP.", zipFilename);
            processedZip.Status = ProcessingStatusConsts.UnableToReadZipError;
            return processedZip;
        }

        using var archive = new ZipArchive(zipFileStream!, ZipArchiveMode.Read);

        _logger.LogInformation("ZIP {zipFilename} | Start reading CSV.", zipFilename);

        var csvItems = GetCsvItems(archive);

        if (csvItems == null)
        {
            _logger.LogWarning("ZIP {zipFilename} | CSV not found.", zipFilename);
            processedZip.Status = ProcessingStatusConsts.CsvNotFoundError;
            return processedZip;
        }

        _logger.LogInformation("ZIP {zipFilename} | End reading CSV.", zipFilename);

        var processedPdfs = new ConcurrentBag<ProcessedPdf>();

        _logger.LogInformation("ZIP {zipFilename} | Start reading PDFs.", zipFilename);

        await Parallel.ForEachAsync
        (
            csvItems,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = cancellationToken
            },
            async (csvItem, _) =>
            {
                foreach (var attachment in csvItem.Attachments)
                {
                    var processedPdf = await ProcessPdf
                    (
                        zipFilename: zipFilename,
                        archive: archive,
                        pdfFilename: attachment.Filename,
                        poNumber: csvItem.PONumber,
                        cancellationToken: cancellationToken
                    );
                    processedPdfs.Add(processedPdf);
                }
            }
        );

        _logger.LogInformation("ZIP {zipFilename} | End reading PDFs.", zipFilename);

        var pdfCount = processedPdfs.Count;
        var pdfSuccessCount = processedPdfs.Count(x => x.Status == ProcessingStatusConsts.Success);

        processedZip.Status = pdfSuccessCount switch
        {
            0 => ProcessingStatusConsts.Error,
            _ when pdfSuccessCount < pdfCount => ProcessingStatusConsts.Partial,
            _ when pdfSuccessCount == pdfCount => ProcessingStatusConsts.Success,
            _ => "",
        };

        processedZip.PdfFiles.AddRange(processedPdfs);

        _logger.LogInformation("End processing ZIP {zipFilename}", zipFilename);

        return processedZip;
    }
}