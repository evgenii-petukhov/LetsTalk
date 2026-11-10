namespace LetsTalk.Server.Configuration.Models;

public class JwtSettings
{
    public string? Key { get; set; }

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan BackgroundTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan RefreshIdleLifetime { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan RefreshAbsoluteLifetime { get; set; } = TimeSpan.FromDays(90);

    public TimeSpan RefreshReuseGrace { get; set; } = TimeSpan.FromSeconds(30);
}
