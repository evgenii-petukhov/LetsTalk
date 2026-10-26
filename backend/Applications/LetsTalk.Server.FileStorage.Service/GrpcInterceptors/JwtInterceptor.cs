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
        // The session cookie wins over the explicit "authorization" metadata (same rule as the API)
        var token = context.GetHttpContext()?.Request.Cookies[_authCookieSettings.Name];
        if (string.IsNullOrEmpty(token))
        {
            token = context.RequestHeaders.Get("authorization")?.Value;
        }

        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException();
        }

        var accountId = await _authenticationClient.ValidateJwtTokenAsync(token);
        if (string.IsNullOrEmpty(accountId))
        {
            throw new UnauthorizedAccessException();
        }

        context.UserState.Add("AccountId", accountId);
        var response = await continuation(request, context);

        _signPackageService.Sign(response);

        return response;
    }
}
