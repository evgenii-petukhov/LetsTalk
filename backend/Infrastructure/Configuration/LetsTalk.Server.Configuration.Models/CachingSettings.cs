namespace LetsTalk.Server.Configuration.Models;

/// <remarks>
/// The five cache lifetimes share one meaning: a positive value is the lifetime in seconds, 0 switches that cache off,
/// and -1 keeps the entries until they are removed (they never expire). Any other negative value is rejected at start.
/// </remarks>
public class CachingSettings
{
    /// <summary>Lifetime of cached message pages, in seconds. 0 = cache off, -1 = never expires.</summary>
    public int MessagesCacheLifeTimeInSeconds { get; set; } = 120;

    /// <summary>Lifetime of cached chats, in seconds. 0 = cache off, -1 = never expires.</summary>
    public int ChatCacheLifeTimeInSeconds { get; set; } = 30;

    /// <summary>Lifetime of cached accounts, in seconds. 0 = cache off, -1 = never expires.</summary>
    public int AccountCacheLifeTimeInSeconds { get; set; } = 30;

    /// <summary>Lifetime of cached profiles, in seconds. 0 = cache off, -1 = never expires.</summary>
    public int ProfileCacheLifeTimeInSeconds { get; set; } = 30;

    /// <summary>Lifetime of cached images, in seconds. 0 = cache off, -1 = never expires.</summary>
    public int ImagesCacheLifeTimeInSeconds { get; set; } = 86400;

    /// <summary>An image is cached only when it is smaller than this many bytes. 0 = no image is cached.</summary>
    public int ImageSizeThresholdInBytes { get; set; } = 102400;

    /// <summary>Lifetime of a login code, in seconds. Allowed: 1 to 3600.</summary>
    public int LoginCodeCacheLifeTimeInSeconds { get; set; } = 120;
}
