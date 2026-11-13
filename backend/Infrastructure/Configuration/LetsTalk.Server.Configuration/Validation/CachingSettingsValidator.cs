using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class CachingSettingsValidator : SettingsValidatorBase<CachingSettings>
{
    public const string Section = "Caching";

    /// <summary>-1 = never expires, 0 = cache off (FR-008).</summary>
    public const int MinCacheLifeTime = -1;
    public const int MaxCacheLifeTime = 604800;
    public const int MinLoginCodeLifeTime = 1;
    public const int MaxLoginCodeLifeTime = 3600;
    public const int MinImageSizeThreshold = 0;
    public const int MaxImageSizeThreshold = 10485760;

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(CachingSettings settings)
    {
        yield return CacheLifeTime(nameof(settings.MessagesCacheLifeTimeInSeconds), settings.MessagesCacheLifeTimeInSeconds);
        yield return CacheLifeTime(nameof(settings.ChatCacheLifeTimeInSeconds), settings.ChatCacheLifeTimeInSeconds);
        yield return CacheLifeTime(nameof(settings.AccountCacheLifeTimeInSeconds), settings.AccountCacheLifeTimeInSeconds);
        yield return CacheLifeTime(nameof(settings.ProfileCacheLifeTimeInSeconds), settings.ProfileCacheLifeTimeInSeconds);
        yield return CacheLifeTime(nameof(settings.ImagesCacheLifeTimeInSeconds), settings.ImagesCacheLifeTimeInSeconds);
        yield return SettingsRules.Range(Key(nameof(settings.LoginCodeCacheLifeTimeInSeconds)),
            settings.LoginCodeCacheLifeTimeInSeconds, MinLoginCodeLifeTime, MaxLoginCodeLifeTime);
        yield return SettingsRules.Range(Key(nameof(settings.ImageSizeThresholdInBytes)),
            settings.ImageSizeThresholdInBytes, MinImageSizeThreshold, MaxImageSizeThreshold);
    }

    private string? CacheLifeTime(string property, int value) =>
        SettingsRules.Range(Key(property), value, MinCacheLifeTime, MaxCacheLifeTime);
}
