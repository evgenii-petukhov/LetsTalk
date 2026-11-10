using LetsTalk.Server.Configuration.Models;
using Microsoft.AspNetCore.Http;

namespace LetsTalk.Server.API.Middleware;

public static class AuthCookieExtensions
{
    private const string AccessCookiePath = "/";

    public static void AppendAuthCookie(
        this HttpResponse response,
        AuthCookieSettings settings,
        string token,
        DateTime expiresAt)
    {
        Append(response, settings, settings.Name, AccessCookiePath, token, expiresAt);
    }

    // Only the sign-in, refresh and sign-out endpoints receive this cookie (RefreshPath)
    public static void AppendRefreshCookie(
        this HttpResponse response,
        AuthCookieSettings settings,
        string token,
        DateTime expiresAt)
    {
        Append(response, settings, settings.RefreshName, settings.RefreshPath, token, expiresAt);
    }

    public static void DeleteAuthCookie(this HttpResponse response, AuthCookieSettings settings)
    {
        response.Cookies.Delete(settings.Name, CreateOptions(settings, AccessCookiePath));
    }

    public static void DeleteRefreshCookie(this HttpResponse response, AuthCookieSettings settings)
    {
        response.Cookies.Delete(settings.RefreshName, CreateOptions(settings, settings.RefreshPath));
    }

    private static void Append(
        HttpResponse response,
        AuthCookieSettings settings,
        string name,
        string path,
        string token,
        DateTime expiresAt)
    {
        var options = CreateOptions(settings, path);
        options.Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc));
        response.Cookies.Append(name, token, options);
    }

    private static CookieOptions CreateOptions(AuthCookieSettings settings, string path)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = Enum.TryParse<SameSiteMode>(settings.SameSite, ignoreCase: true, out var sameSite)
                ? sameSite
                : SameSiteMode.Strict,
            Domain = string.IsNullOrEmpty(settings.Domain) ? null : settings.Domain,
            Path = path
        };
    }
}
