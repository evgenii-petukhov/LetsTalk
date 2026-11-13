using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using LetsTalk.Server.Persistence.Enums;
using LetsTalk.Server.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.FileStorage.Amazon.Services;

public static class AmazonFileServicesRegistration
{
    public static IServiceCollection AddAmazonFileServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IFileService, AmazonFileService>();
        // Both file services are always registered, and the storage is chosen per file. The Aws values are
        // required only where Amazon S3 is the configured storage; a local set-up needs none of them.
        if (FileStorageMode.Of(configuration) == FileStorageTypes.AmazonS3)
        {
            services.AddValidatedSettings<AwsSettings, AwsSettingsValidator>(configuration, AwsSettingsValidator.Section);
        }
        else
        {
            services.Configure<AwsSettings>(configuration.GetSection("Aws"));
        }

        return services;
    }
}
