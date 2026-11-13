using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

/// <summary>Required values with no default and no entry in any appsettings.json. A message never shows a value.</summary>
public class AwsSettingsValidator : SettingsValidatorBase<AwsSettings>
{
    public const string Section = "Aws";

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(AwsSettings settings)
    {
        yield return SettingsRules.NotEmpty(Key(nameof(settings.AccessKey)), settings.AccessKey);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.SecretKey)), settings.SecretKey);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.Region)), settings.Region);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.BucketName)), settings.BucketName);
    }
}
