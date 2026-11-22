using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class HttpRetrySettingsValidator : SettingsValidatorBase<HttpRetrySettings>
{
    public const string Section = "Resilience:Http";

    public const int MinMaxRetryAttempts = 0;
    public const int MaxMaxRetryAttempts = 10;
    public static readonly TimeSpan MinDelay = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan MaxDelayLimit = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MaxDelayOfFirstPause = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MinAttemptTimeout = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan MaxAttemptTimeout = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan MaxTotalTimeout = TimeSpan.FromMinutes(30);

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(HttpRetrySettings settings)
    {
        yield return SettingsRules.Range(
            Key(nameof(settings.MaxRetryAttempts)),
            settings.MaxRetryAttempts,
            MinMaxRetryAttempts,
            MaxMaxRetryAttempts);

        yield return SettingsRules.Range(
            Key(nameof(settings.Delay)),
            settings.Delay,
            MinDelay,
            MaxDelayOfFirstPause);

        // The longest pause is never shorter than the first one
        yield return SettingsRules.Range(
            Key(nameof(settings.MaxDelay)),
            settings.MaxDelay,
            settings.Delay > MinDelay ? settings.Delay : MinDelay,
            MaxDelayLimit);

        yield return SettingsRules.Range(
            Key(nameof(settings.AttemptTimeout)),
            settings.AttemptTimeout,
            MinAttemptTimeout,
            MaxAttemptTimeout);

        // The whole call is never shorter than one attempt
        yield return SettingsRules.Range(
            Key(nameof(settings.TotalTimeout)),
            settings.TotalTimeout,
            settings.AttemptTimeout > MinAttemptTimeout ? settings.AttemptTimeout : MinAttemptTimeout,
            MaxTotalTimeout);
    }
}
