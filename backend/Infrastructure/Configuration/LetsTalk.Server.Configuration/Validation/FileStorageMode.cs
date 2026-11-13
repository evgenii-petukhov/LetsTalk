using LetsTalk.Server.Persistence.Enums;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration.Validation;

public static class FileStorageMode
{
    /// <summary>
    /// The file storage that <c>Features:FileStorage</c> selects, read the same way as
    /// <c>FeaturesSettingsService</c>: an absent or unknown value means <see cref="FileStorageTypes.Local"/>.
    /// </summary>
    public static FileStorageTypes Of(IConfiguration configuration) =>
        Enum.TryParse<FileStorageTypes>(configuration.GetValue<string>("Features:FileStorage"), out var type)
            ? type
            : FileStorageTypes.Local;
}
