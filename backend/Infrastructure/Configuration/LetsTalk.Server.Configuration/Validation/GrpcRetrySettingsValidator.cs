using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class GrpcRetrySettingsValidator : SettingsValidatorBase<GrpcRetrySettings>
{
    public const string Section = "Resilience:Grpc";

    /// <summary>The gRPC retry policy allows at most five attempts.</summary>
    public const int MinMaxAttempts = 1;
    public const int MaxMaxAttempts = 5;
    public static readonly TimeSpan MinBackoff = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan MaxInitialBackoff = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxBackoffLimit = TimeSpan.FromMinutes(5);

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(GrpcRetrySettings settings)
    {
        yield return SettingsRules.Range(
            Key(nameof(settings.MaxAttempts)),
            settings.MaxAttempts,
            MinMaxAttempts,
            MaxMaxAttempts);

        yield return SettingsRules.Range(
            Key(nameof(settings.InitialBackoff)),
            settings.InitialBackoff,
            MinBackoff,
            MaxInitialBackoff);

        yield return SettingsRules.Range(
            Key(nameof(settings.MaxBackoff)),
            settings.MaxBackoff,
            settings.InitialBackoff > MinBackoff ? settings.InitialBackoff : MinBackoff,
            MaxBackoffLimit);
    }
}
