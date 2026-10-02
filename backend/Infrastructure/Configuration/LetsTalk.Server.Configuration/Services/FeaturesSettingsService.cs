using LetsTalk.Server.Configuration.Abstractions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Persistence.Enums;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Services;

public class FeaturesSettingsService(IOptions<FeaturesSettings> options) : IFeaturesSettingsService
{
    private readonly IOptions<FeaturesSettings> _options = options ?? throw new ArgumentNullException(nameof(options));

    public FileStorageTypes GetFileStorageType()
    {
        var fileStorage = _options.Value?.FileStorage;

        if (string.IsNullOrWhiteSpace(fileStorage))
        {
            return FileStorageTypes.Local;
        }

        return Enum.TryParse<FileStorageTypes>(fileStorage, out var fileStorageType)
            ? fileStorageType
            : FileStorageTypes.Local;
    }
}
