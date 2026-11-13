using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

/// <summary>The mail server settings have no defaults and no entry in any appsettings.json. A message never shows a value.</summary>
public class EmailServiceSettingsValidator : SettingsValidatorBase<EmailServiceSettings>
{
    public const string Section = "EmailService";
    public const int MinPort = 1;
    public const int MaxPort = 65535;

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(EmailServiceSettings settings)
    {
        yield return SettingsRules.NotEmpty(Key(nameof(settings.SenderEmail)), settings.SenderEmail);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.SenderName)), settings.SenderName);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.Server)), settings.Server);
        yield return SettingsRules.Range(Key(nameof(settings.Port)), settings.Port, MinPort, MaxPort);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.UserName)), settings.UserName);
        yield return SettingsRules.NotEmpty(Key(nameof(settings.Password)), settings.Password);
    }
}
