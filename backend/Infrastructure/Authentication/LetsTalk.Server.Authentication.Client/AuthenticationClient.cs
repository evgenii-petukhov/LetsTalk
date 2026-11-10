using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using static LetsTalk.Server.Authentication.JwtTokenGrpcService;
using static LetsTalk.Server.Authentication.LoginCodeGrpcService;

namespace LetsTalk.Server.Authentication.Client;

public class AuthenticationClient(
    JwtTokenGrpcServiceClient tokenClient,
    LoginCodeGrpcServiceClient loginCodeClient) : IAuthenticationClient
{
    private readonly JwtTokenGrpcServiceClient _tokenClient = tokenClient;
    private readonly LoginCodeGrpcServiceClient _loginCodeClient = loginCodeClient;

    public async Task<(string token, DateTime expiresAt)> GenerateJwtTokenAsync(string accountId)
    {
        var response = await _tokenClient.GenerateJwtTokenAsync(new GenerateJwtTokenRequest
        {
            AccountId = accountId
        });

        return (response.Token, ToUtc(response.ExpiresAtUnixSeconds));
    }

    public async Task<(string token, DateTime expiresAt)> GenerateBackgroundTokenAsync(string accountId)
    {
        var response = await _tokenClient.GenerateJwtTokenAsync(new GenerateJwtTokenRequest
        {
            AccountId = accountId,
            Kind = TokenKind.Background
        });

        return (response.Token, ToUtc(response.ExpiresAtUnixSeconds));
    }

    public async Task<AuthSession> CreateSessionAsync(string accountId)
    {
        var response = await _tokenClient.CreateSessionAsync(new CreateSessionRequest
        {
            AccountId = accountId
        });

        return ToSession(response);
    }

    public async Task<AuthSession?> RefreshSessionAsync(string refreshToken)
    {
        try
        {
            var response = await _tokenClient.RefreshSessionAsync(new RefreshSessionRequest
            {
                RefreshToken = refreshToken
            });

            return ToSession(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            return null;
        }
    }

    public async Task EndSessionAsync(string refreshToken)
    {
        await _tokenClient.EndSessionAsync(new EndSessionRequest
        {
            RefreshToken = refreshToken
        });
    }

    public async Task<string?> ValidateJwtTokenAsync(string token)
    {
        var response = await _tokenClient.ValidateJwtTokenAsync(new ValidateJwtTokenRequest
        {
            Token = token
        });

        return response.AccountId;
    }

    public async Task RevokeJwtTokenAsync(string token)
    {
        await _tokenClient.RevokeJwtTokenAsync(new RevokeJwtTokenRequest
        {
            Token = token
        });
    }

    public async Task<(int code, bool isCodeCreated, int ttl)> GenerateLoginCodeAsync(string email)
    {
        var response = await _loginCodeClient.GenerateLoginCodeAsync(new GenerateLoginCodeRequest
        {
            Email = email
        });

        return (response.Code, response.IsCodeCreated, response.Ttl);
    }

    public async Task<bool> ValidateLoginCodeAsync(string email, int code)
    {
        var response = await _loginCodeClient.ValidateLoginCodeAsync(new ValidateLoginCodeRequest
        {
            Email = email,
            Code = code
        });

        return response.IsValid;
    }

    private static DateTime ToUtc(long unixSeconds) => DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;

    private static AuthSession ToSession(SessionResponse response)
    {
        var hasRefreshToken = !string.IsNullOrEmpty(response.RefreshToken);

        return new AuthSession(
            response.AccessToken,
            ToUtc(response.AccessExpiresAtUnixSeconds),
            hasRefreshToken ? response.RefreshToken : null,
            hasRefreshToken ? ToUtc(response.RefreshExpiresAtUnixSeconds) : null);
    }
}
