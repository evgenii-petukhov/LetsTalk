namespace LetsTalk.Server.Resilience;

/// <summary>
/// How a call over HTTP is repeated (spec 006). The defaults are the shipped values; a Lambda has no configuration
/// and uses them as they are.
/// </summary>
public class HttpRetrySettings
{
    /// <summary>How many times a failed call is repeated; 0 = no repeat.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>The first pause; it doubles with each repeat.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>The longest pause, also when the answer asks for a longer one.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>The longest wait for one attempt.</summary>
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>The longest wait for the whole call, all attempts and pauses included.</summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(100);
}
