using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Cors;
using LetsTalk.Server.Notifications.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Notifications.Hubs;

public class NotificationHub(
    IConnectionManager connectionManager,
    IAuthenticationClient authenticationClient,
    IOptions<AuthCookieSettings> authCookieSettingsOptions,
    AllowedOrigins allowedOrigins) : Hub<INotificationHub>
{
    private readonly IConnectionManager _connectionManager = connectionManager;
    private readonly IAuthenticationClient _authenticationClient = authenticationClient;
    private readonly AuthCookieSettings _authCookieSettings = authCookieSettingsOptions.Value;
    private readonly AllowedOrigins _allowedOrigins = allowedOrigins;

    public override async Task OnConnectedAsync()
    {
        // The browser sends the session cookie with the WebSocket handshake. Without a cookie the
        // connection stays unregistered until a client with an explicit credential calls AuthorizeAsync.
        var httpContext = Context.GetHttpContext();
        var token = httpContext?.Request.Cookies[_authCookieSettings.Name];
        if (!string.IsNullOrEmpty(token))
        {
            // WebSockets are not governed by CORS, so a page on another site could otherwise open the
            // connection with the user's cookie. Browsers always send the Origin header with the handshake.
            if (!_allowedOrigins.Contains(httpContext!.Request.Headers.Origin.ToString()))
            {
                Context.Abort();
                return;
            }

            var accountId = await TryValidateAsync(token);
            if (accountId == null)
            {
                Context.Abort();
                return;
            }

            _connectionManager.AddConnectionId(accountId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public async Task AuthorizeAsync(string token)
    {
        var accountId = await _authenticationClient.ValidateJwtTokenAsync(token);
        if (accountId != null)
        {
            _connectionManager.AddConnectionId(accountId, Context.ConnectionId);
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionManager.RemoveConnectionId(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private async Task<string?> TryValidateAsync(string token)
    {
        try
        {
            return await _authenticationClient.ValidateJwtTokenAsync(token);
        }
        catch (RpcException)
        {
            return null;
        }
    }
}
