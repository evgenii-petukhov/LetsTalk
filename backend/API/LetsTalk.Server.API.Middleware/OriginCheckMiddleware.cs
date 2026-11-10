using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.API.Middleware;

// A browser attaches its cookies to every request to our hosts, also to those started by other sites.
// A state-changing request that carries the browser session is therefore accepted only when it comes
// from one of the web app origins. Requests with an explicit credential (Authorization header) are
// exempt, because the browser never adds that header on its own.
public class OriginCheckMiddleware(RequestDelegate next, AllowedOrigins allowedOrigins)
{
    private readonly RequestDelegate _next = next;
    private readonly AllowedOrigins _allowedOrigins = allowedOrigins;

    public async Task InvokeAsync(HttpContext context, IOptions<AuthCookieSettings> cookieSettingsOptions)
    {
        if (IsSafeMethod(context.Request.Method) || !CarriesBrowserSession(context, cookieSettingsOptions.Value))
        {
            await _next(context);
            return;
        }

        if (!_allowedOrigins.Contains(GetRequestOrigin(context.Request)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "Forbidden" });
            return;
        }

        await _next(context);
    }

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method)
        || HttpMethods.IsHead(method)
        || HttpMethods.IsOptions(method)
        || HttpMethods.IsTrace(method);

    // Refresh and sign-out arrive with the refresh cookie only once the access cookie has expired
    private static bool CarriesBrowserSession(HttpContext context, AuthCookieSettings cookieSettings) =>
        context.Items[CredentialSources.ItemKey] as string == CredentialSources.Cookie
        || context.Request.Cookies.ContainsKey(cookieSettings.RefreshName);

    private static string? GetRequestOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            return origin;
        }

        // Some browsers omit Origin on same-origin requests, but still send the Referer
        if (Uri.TryCreate(request.Headers.Referer.ToString(), UriKind.Absolute, out var referer)
            && (referer.Scheme == Uri.UriSchemeHttps || referer.Scheme == Uri.UriSchemeHttp))
        {
            return $"{referer.Scheme}://{referer.Authority}";
        }

        return null;
    }
}
