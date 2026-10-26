using LetsTalk.Server.Configuration.Models;
using Microsoft.AspNetCore.Http;

namespace LetsTalk.Server.API.Middleware;

public static class AuthCookieExtensions
{
    public static void AppendAuthCookie(
        this HttpResponse response,
        AuthCookieSettings settings,
        string token,
        DateTime expiresAt)
    {
        var options = CreateOptions(settings);
        options.Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc));
        response.Cookies.Append(settings.Name, token, options);
    }

    public static void DeleteAuthCookie(this HttpResponse response, AuthCookieSettings settings)
    {
        response.Cookies.Delete(settings.Name, CreateOptions(settings));
    }

    private static CookieOptions CreateOptions(AuthCookieSettings settings)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = Enum.TryParse<SameSiteMode>(settings.SameSite, ignoreCase: true, out var sameSite)
                ? sameSite
                : SameSiteMode.Strict,
            Domain = string.IsNullOrEmpty(settings.Domain) ? null : settings.Domain,
            Path = "/"
        };
    }
}
