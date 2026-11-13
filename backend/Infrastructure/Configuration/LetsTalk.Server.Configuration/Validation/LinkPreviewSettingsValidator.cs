using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

/// <summary>Required values with no default and no entry in any appsettings.json. A message never shows a value.</summary>
public class LinkPreviewSettingsValidator : SettingsValidatorBase<LinkPreviewSettings>
{
    public const string Section = "LinkPreview";

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(LinkPreviewSettings settings)
    {
        yield return SettingsRules.NotEmpty(Key(nameof(settings.SecretKey)), settings.SecretKey);
    }
}
