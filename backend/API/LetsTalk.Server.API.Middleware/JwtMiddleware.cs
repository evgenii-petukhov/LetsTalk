using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.API.Middleware;

public class JwtMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IAuthenticationClient authenticationClient,
        IOptions<AuthCookieSettings> cookieSettingsOptions)
    {
        var cookieSettings = cookieSettingsOptions.Value;

        var cookieToken = context.Request.Cookies[cookieSettings.Name];
        if (!string.IsNullOrEmpty(cookieToken))
        {
            var accountId = await TryValidateCookieTokenAsync(context, authenticationClient, cookieSettings, cookieToken);
            if (accountId != null)
            {
                context.Items["AccountId"] = accountId;
                context.Items["Token"] = cookieToken;
                await _next(context);
                return;
            }
        }

        var headerToken = context.Request.Headers.Authorization.FirstOrDefault()?
            .Split(" ")
            .Last();

        if (!string.IsNullOrEmpty(headerToken))
        {
            try
            {
                context.Items["AccountId"] = await authenticationClient.ValidateJwtTokenAsync(headerToken)
                    ?? throw new UnauthorizedAccessException("Invalid token");
                context.Items["Token"] = headerToken;
            }
            catch (RpcException)
            {
                throw new UnauthorizedAccessException("Invalid token");
            }
        }

        await _next(context);
    }

    // A session cookie that is no longer valid is treated as "not signed in" and removed from the browser,
    // because page scripts cannot remove an HttpOnly cookie. A temporarily unreachable
    // Authentication service must not sign users out, so the cookie is kept in that case.
    private static async Task<string?> TryValidateCookieTokenAsync(
        HttpContext context,
        IAuthenticationClient authenticationClient,
        AuthCookieSettings cookieSettings,
        string cookieToken)
    {
        try
        {
            var accountId = await authenticationClient.ValidateJwtTokenAsync(cookieToken);
            if (accountId == null)
            {
                context.Response.DeleteAuthCookie(cookieSettings);
            }

            return accountId;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return null;
        }
        catch (RpcException)
        {
            context.Response.DeleteAuthCookie(cookieSettings);
            return null;
        }
    }
}
