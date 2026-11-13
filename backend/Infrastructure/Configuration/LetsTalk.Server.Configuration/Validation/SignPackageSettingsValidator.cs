using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

/// <summary>Required values with no default and no entry in any appsettings.json. A message never shows a value.</summary>
public class SignPackageSettingsValidator : SettingsValidatorBase<SignPackageSettings>
{
    public const string Section = "SignPackage";

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(SignPackageSettings settings)
    {
        yield return SettingsRules.NotEmpty(Key(nameof(settings.Pepper)), settings.Pepper);
    }
}
