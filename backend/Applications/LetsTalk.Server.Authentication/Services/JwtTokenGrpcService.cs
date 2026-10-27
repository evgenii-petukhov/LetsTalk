using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using static LetsTalk.Server.Authentication.JwtTokenGrpcService;

namespace LetsTalk.Server.Authentication.Services;

public class JwtTokenGrpcService(IJwtCacheService jwtCacheService) : JwtTokenGrpcServiceBase
{
    private readonly IJwtCacheService _jwtCacheService = jwtCacheService;

    public override async Task<GenerateJwtTokenResponse> GenerateJwtToken(GenerateJwtTokenRequest request, ServerCallContext context)
    {
        var storedToken = await _jwtCacheService.GenerateAsync(request.AccountId);

        return new GenerateJwtTokenResponse
        {
            Token = storedToken.Token,
            ExpiresAtUnixSeconds = new DateTimeOffset(storedToken.ValidTo).ToUnixTimeSeconds()
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
}