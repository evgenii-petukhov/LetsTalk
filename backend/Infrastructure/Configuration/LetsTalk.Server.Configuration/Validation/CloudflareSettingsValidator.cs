using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class CloudflareSettingsValidator : SettingsValidatorBase<CloudflareSettings>
{
    public const string Section = "Cloudflare";
    public const int MinTokenTtl = 60;
    // Cloudflare documents no maximum; 172800 (48 hours) is the figure of two third-party references (research.md, section 5)
    public const int MaxTokenTtl = 172800;

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(CloudflareSettings settings)
    {
        yield return SettingsRules.NotEmpty(Key(nameof(settings.TurnTokenId)), settings.TurnTokenId);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.ApiToken)), settings.ApiToken);
        yield return SettingsRules.Range(Key(nameof(settings.TokenTtl)), settings.TokenTtl, MinTokenTtl, MaxTokenTtl);
    }
}
