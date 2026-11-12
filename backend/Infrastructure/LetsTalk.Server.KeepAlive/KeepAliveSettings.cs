namespace LetsTalk.Server.KeepAlive;

// Configuration section "KeepAlive". A store is pinged only when it is listed under Targets
public class KeepAliveSettings
{
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(30);

    // Settings of each store, keyed by IKeepAliveTarget.Name ("Redis", "MongoDB"), ignoring letter case.
    // A store is pinged only when it has an entry here (opt-in).
    public Dictionary<string, KeepAliveTargetSettings> Targets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
