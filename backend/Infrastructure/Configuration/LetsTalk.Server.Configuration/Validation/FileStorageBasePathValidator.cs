using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

/// <summary>
/// <see cref="FileStorageSettings.BasePath"/> is a deployment value with no default. It is checked only in a host
/// that stores files locally (see <see cref="FileStorageSettingsRegistration.AddFileStorageSettings"/>).
/// </summary>
public class FileStorageBasePathValidator : SettingsValidatorBase<FileStorageSettings>
{
    protected override string SectionName => FileStorageSettingsValidator.Section;

    protected override IEnumerable<string?> GetErrors(FileStorageSettings settings)
    {
        yield return SettingsRules.NotEmpty(Key(nameof(settings.BasePath)), settings.BasePath);
    }
}
