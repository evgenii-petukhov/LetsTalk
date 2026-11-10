using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Options;
using static LetsTalk.Server.Authentication.JwtTokenGrpcService;

namespace LetsTalk.Server.Authentication.Services;

public class JwtTokenGrpcService(
    IJwtCacheService jwtCacheService,
    IRefreshSessionService refreshSessionService,
    IOptions<JwtSettings> jwtSettings) : JwtTokenGrpcServiceBase
{
    private readonly IJwtCacheService _jwtCacheService = jwtCacheService;
    private readonly IRefreshSessionService _refreshSessionService = refreshSessionService;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public override async Task<GenerateJwtTokenResponse> GenerateJwtToken(GenerateJwtTokenRequest request, ServerCallContext context)
    {
        var lifetime = request.Kind == TokenKind.Background
            ? _jwtSettings.BackgroundTokenLifetime
            : _jwtSettings.AccessTokenLifetime;
        var storedToken = await _jwtCacheService.GenerateAsync(request.AccountId, lifetime);

        return new GenerateJwtTokenResponse
        {
            Token = storedToken.Token,
            ExpiresAtUnixSeconds = ToUnixSeconds(storedToken.ValidTo)
        };
    }

    public override async Task<ValidateJwtTokenResponse> ValidateJwtToken(ValidateJwtTokenRequest request, ServerCallContext context)
    {
        var accountId = await _jwtCacheService.GetAccountIdAsync(request.Token);

        return new ValidateJwtTokenResponse
        {
            AccountId = accountId
        };
    }

    public override async Task<RevokeJwtTokenResponse> RevokeJwtToken(RevokeJwtTokenRequest request, ServerCallContext context)
    {
        await _jwtCacheService.RevokeAsync(request.Token);

        return new RevokeJwtTokenResponse();
    }

    public override async Task<SessionResponse> CreateSession(CreateSessionRequest request, ServerCallContext context)
    {
        var tokens = await _refreshSessionService.IssueAsync(request.AccountId);

        return ToResponse(tokens);
    }

    public override async Task<SessionResponse> RefreshSession(RefreshSessionRequest request, ServerCallContext context)
    {
        var tokens = await _refreshSessionService.RotateAsync(request.RefreshToken)
            ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "The refresh token is not valid"));

        return ToResponse(tokens);
    }

    public override async Task<EndSessionResponse> EndSession(EndSessionRequest request, ServerCallContext context)
    {
        await _refreshSessionService.EndByRefreshTokenAsync(request.RefreshToken);

        return new EndSessionResponse();
    }

    private static SessionResponse ToResponse(SessionTokens tokens)
    {
        return new SessionResponse
        {
            AccessToken = tokens.AccessToken,
            AccessExpiresAtUnixSeconds = ToUnixSeconds(tokens.AccessExpiresAt),
            RefreshToken = tokens.RefreshToken ?? string.Empty,
            RefreshExpiresAtUnixSeconds = tokens.RefreshExpiresAt.HasValue ? ToUnixSeconds(tokens.RefreshExpiresAt.Value) : 0
        };
    }

    private static long ToUnixSeconds(DateTime utc) => new DateTimeOffset(utc).ToUnixTimeSeconds();
}
