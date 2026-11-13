using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class MessagingSettingsValidator : SettingsValidatorBase<MessagingSettings>
{
    public const string Section = "Messaging";
    public const int MinMessagesPerPage = 1;
    public const int MaxMessagesPerPage = 100;

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(MessagingSettings settings)
    {
        yield return SettingsRules.Range(Key(nameof(settings.MessagesPerPage)), settings.MessagesPerPage,
            MinMessagesPerPage, MaxMessagesPerPage);
    }
}
