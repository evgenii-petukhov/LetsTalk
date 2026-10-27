using Grpc.Core;
using Grpc.Core.Interceptors;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.SignPackage.Abstractions;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.FileStorage.Service.GrpcInterceptors;

public class JwtInterceptor(
    IAuthenticationClient authenticationClient,
    ISignPackageService signPackageService,
    IOptions<AuthCookieSettings> authCookieSettingsOptions) : Interceptor
{
    private readonly IAuthenticationClient _authenticationClient = authenticationClient;
    private readonly ISignPackageService _signPackageService = signPackageService;
    private readonly AuthCookieSettings _authCookieSettings = authCookieSettingsOptions.Value;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var accountId = await GetAccountIdAsync(context);
        if (string.IsNullOrEmpty(accountId))
        {
            throw new UnauthorizedAccessException();
        }

        context.UserState.Add("AccountId", accountId);
        var response = await continuation(request, context);

        _signPackageService.Sign(response);

        return response;
    }

    // Same rule as the API: the session cookie first, then the explicit "authorization" metadata.
    // An invalid cookie does not hide a valid explicit credential.
    private async Task<string?> GetAccountIdAsync(ServerCallContext context)
    {
        var cookieToken = context.GetHttpContext()?.Request.Cookies[_authCookieSettings.Name];
        var metadataToken = context.RequestHeaders.Get("authorization")?.Value;

        foreach (var token in new[] { cookieToken, metadataToken })
        {
            if (string.IsNullOrEmpty(token))
            {
                continue;
            }

            var accountId = await TryValidateAsync(token);
            if (!string.IsNullOrEmpty(accountId))
            {
                return accountId;
            }
        }

        return null;
    }

    // The Authentication service reports an invalid token as an RpcException. An outage
    // (Unavailable, DeadlineExceeded) is not an invalid credential and propagates as it is.
    private async Task<string?> TryValidateAsync(string token)
    {
        try
        {
            return await _authenticationClient.ValidateJwtTokenAsync(token);
        }
        catch (RpcException ex) when (ex.StatusCode is not (StatusCode.Unavailable or StatusCode.DeadlineExceeded))
        {
            return null;
        }
    }
}
