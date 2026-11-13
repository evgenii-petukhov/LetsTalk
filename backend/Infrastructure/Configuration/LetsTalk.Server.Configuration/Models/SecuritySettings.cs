namespace LetsTalk.Server.Configuration.Models;

public class SecuritySettings
{
    /// <summary>How long an anti-spam token is accepted, in seconds. Allowed: 1 to 3600.</summary>
    public int AntiSpamTokenLifeTimeInSeconds { get; set; } = 60;
}
