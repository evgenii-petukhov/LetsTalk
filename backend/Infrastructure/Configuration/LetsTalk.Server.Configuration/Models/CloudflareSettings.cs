namespace LetsTalk.Server.Configuration.Models;

public class CloudflareSettings
{
    public string? TurnTokenId { get; set; }

    public string? ApiToken { get; set; }

    /// <summary>Lifetime of the TURN credentials and of their cache entry, in seconds. Allowed: 60 to 172800.</summary>
    public int TokenTtl { get; set; } = 86400;
}
