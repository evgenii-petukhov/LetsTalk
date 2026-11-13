using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class SecuritySettingsValidator : SettingsValidatorBase<SecuritySettings>
{
    public const string Section = "Security";
    public const int MinAntiSpamTokenLifeTimeInSeconds = 1;
    public const int MaxAntiSpamTokenLifeTimeInSeconds = 3600;

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(SecuritySettings settings)
    {
        yield return SettingsRules.Range(Key(nameof(settings.AntiSpamTokenLifeTimeInSeconds)),
            settings.AntiSpamTokenLifeTimeInSeconds,
            MinAntiSpamTokenLifeTimeInSeconds, MaxAntiSpamTokenLifeTimeInSeconds);
    }
}
