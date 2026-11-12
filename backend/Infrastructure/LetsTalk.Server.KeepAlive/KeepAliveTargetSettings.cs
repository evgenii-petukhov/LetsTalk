namespace LetsTalk.Server.KeepAlive;

public class KeepAliveTargetSettings
{
    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);
}
