using System.Buffers;
using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

/// <summary>Checks <see cref="FileStorageSettings.ImageFolder"/>: a relative folder name that stays inside the base path.</summary>
public class FileStorageSettingsValidator : SettingsValidatorBase<FileStorageSettings>
{
    public const string Section = "FileStorage";

    // Not taken from Path.GetInvalidFileNameChars(): that differs by operating system, and the same
    // configuration file must be valid on every one of them.
    private static readonly SearchValues<char> InvalidChars = SearchValues.Create("<>:\"|?*");

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(FileStorageSettings settings)
    {
        var key = Key(nameof(settings.ImageFolder));
        var folder = settings.ImageFolder;

        if (string.IsNullOrWhiteSpace(folder))
        {
            yield return $"{key} must be set.";
            yield break;
        }

        if (folder[0] is '/' or '\\')
        {
            yield return $"{key} must be a relative folder name.";
        }

        if (folder.Split('/', '\\').Contains(".."))
        {
            yield return $"{key} must not contain '..'.";
        }

        if (folder.AsSpan().IndexOfAny(InvalidChars) >= 0 || folder.Any(char.IsControl))
        {
            yield return $"{key} contains a character that is not allowed in a folder name.";
        }
    }
}
