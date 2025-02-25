using Amazon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stat.App.Configs;
using Stat.App.Managers;
using Stat.App.Services;
using Stat.Core.BlobStorage;
using Stat.Core.BlobStorage.Aws;

/* ** for easy configuration ********************************* */
Environment.SetEnvironmentVariable("StorageProvider", "AWS");

Environment.SetEnvironmentVariable("AWS-S3__AccessKey", "");
Environment.SetEnvironmentVariable("AWS-S3__SecretKey", "");
Environment.SetEnvironmentVariable("AWS-S3__Region", "");

Environment.SetEnvironmentVariable("Container", "");
/* *********************************************************** */

var host = Host
    .CreateDefaultBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();

#if DEBUG
        AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
        AWSConfigs.LoggingConfig.LogMetrics = true;
        AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.OnError;
# endif
    })
    .ConfigureServices((context, services) =>
    {
        ConfigureBlobStorage(context, services);

        services.Configure<AppConfig>(config =>
        {
            config.Container = context.Configuration["Container"]!;
            config.UnknownPOFolderName = "_";
        });

        services.AddTransient<ProcessingMetadataManager>();
        services.AddTransient<AppService>();
    })
    .Build();

static void ConfigureBlobStorage(HostBuilderContext context, IServiceCollection services)
{
    var storageProvider = context.Configuration["StorageProvider"];

    switch (storageProvider)
    {
        case "AWS":
            services.Configure<AwsS3BlobStorageOptions>(context.Configuration.GetSection("AWS-S3"));
            services.AddTransient<IBlobStorage, AwsS3BlobStorage>();
            break;

        default:
            throw new NotImplementedException();
    }
}

var app = host.Services.GetRequiredService<AppService>();

await app.ProcessNewFilesAsync();
