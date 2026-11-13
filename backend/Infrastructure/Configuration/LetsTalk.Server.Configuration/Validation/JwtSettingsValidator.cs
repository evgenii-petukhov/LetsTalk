using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class JwtSettingsValidator : SettingsValidatorBase<JwtSettings>
{
    public const string Section = "JwtSettings";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits; as ASCII, one character is one byte.</summary>
    public const int MinKeyBytes = 32;

    public static readonly TimeSpan MinTokenLifetime = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxTokenLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan MinRefreshIdleLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaxRefreshLifetime = TimeSpan.FromDays(365);
    public static readonly TimeSpan MaxRefreshReuseGrace = TimeSpan.FromMinutes(5);

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(JwtSettings settings)
    {
        // Neither the key nor its length is part of the message
        // The signing code turns the key into bytes as ASCII, so one character is one byte and any other character
        // would silently become '?'; the key must be ASCII only.
        if (string.IsNullOrWhiteSpace(settings.Key) || settings.Key.Length < MinKeyBytes || !settings.Key.All(char.IsAscii))
        {
            yield return $"{Key(nameof(settings.Key))} must be at least {MinKeyBytes} bytes ({MinKeyBytes * 8} bits) " +
                "of ASCII characters; the configured key is missing, shorter, or has other characters.";
        }

        yield return SettingsRules.Range(Key(nameof(settings.AccessTokenLifetime)), settings.AccessTokenLifetime,
            MinTokenLifetime, MaxTokenLifetime);
        yield return SettingsRules.Range(Key(nameof(settings.BackgroundTokenLifetime)), settings.BackgroundTokenLifetime,
            MinTokenLifetime, MaxTokenLifetime);
        yield return SettingsRules.Range(Key(nameof(settings.RefreshIdleLifetime)), settings.RefreshIdleLifetime,
            MinRefreshIdleLifetime, MaxRefreshLifetime);

        // The absolute lifetime may not be shorter than the idle one
        var minAbsolute = settings.RefreshIdleLifetime > MinRefreshIdleLifetime
            ? settings.RefreshIdleLifetime
            : MinRefreshIdleLifetime;
        yield return SettingsRules.Range(Key(nameof(settings.RefreshAbsoluteLifetime)), settings.RefreshAbsoluteLifetime,
            minAbsolute, MaxRefreshLifetime);

        yield return SettingsRules.Range(Key(nameof(settings.RefreshReuseGrace)), settings.RefreshReuseGrace,
            TimeSpan.Zero, MaxRefreshReuseGrace);
    }
}
