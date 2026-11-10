using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Cors;

// The web app origins from Cors:AllowedOrigins. CORS uses them for the browser's cross-origin calls, and the
// checks that CORS does not cover (state-changing requests with a cookie, the WebSocket handshake) use the same
// list through this class, so every place applies the same rules.
public class AllowedOrigins(IConfiguration configuration)
{
    private readonly HashSet<string> _origins = (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .Select(Normalize)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // Host names are case-insensitive and a trailing slash does not change an origin
    public bool Contains(string? origin) =>
        !string.IsNullOrEmpty(origin) && _origins.Contains(Normalize(origin));

    private static string Normalize(string origin) => origin.TrimEnd('/');
}
