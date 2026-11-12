namespace LetsTalk.Server.KeepAlive;

// Configuration section "KeepAlive". Every value has a default in code, so no appsettings entry is needed.
public class KeepAliveSettings
{
    public bool Enabled { get; set; } = true;

    // The free-tier limits are 14 days (Redis Cloud) and 30 days (MongoDB Atlas), so a day leaves a wide margin.
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);

    // Lets the stores connect before the first signal.
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(30);
}
