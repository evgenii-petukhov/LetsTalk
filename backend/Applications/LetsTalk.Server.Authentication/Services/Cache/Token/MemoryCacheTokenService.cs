using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Exceptions;
using LetsTalk.Server.Authentication.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LetsTalk.Server.Authentication.Services.Cache.Token;

public class MemoryCacheTokenService(
    IJwtStorageService jwtStorageService,
    IMemoryCache memoryCache) : CacheTokenServiceBase, IJwtCacheService
{
    private readonly IJwtStorageService _jwtStorageService = jwtStorageService;
    private readonly IMemoryCache _memoryCache = memoryCache;

    public async Task<string?> GetAccountIdAsync(string? token)
    {
        if (token == null || _memoryCache.TryGetValue(GetRevokedKey(token), out _))
        {
            return null;
        }

        return await _memoryCache.GetOrCreateAsync(GetTokenKey(token), async cacheEntry =>
            {
                var storedToken = await _jwtStorageService.GetStoredTokenAsync(token);
                cacheEntry.AbsoluteExpiration = storedToken!.ValidTo;
                return storedToken.AccountId;
            });
    }

    public ValueTask<StoredToken> GenerateAsync(string accountId)
    {
        var storedToken = _jwtStorageService.Generate(accountId);
        _memoryCache.Set(GetTokenKey(storedToken.Token!), storedToken.AccountId, storedToken.ValidTo);
        return new ValueTask<StoredToken>(storedToken);
    }

    public async Task RevokeAsync(string token)
    {
        StoredToken? storedToken;
        try
        {
            storedToken = await _jwtStorageService.GetStoredTokenAsync(token);
        }
        catch (Exception ex) when (ex is AppException or ArgumentException)
        {
            return;
        }

        if (storedToken == null || storedToken.ValidTo <= DateTime.UtcNow)
        {
            return;
        }

        _memoryCache.Remove(GetTokenKey(token));
        _memoryCache.Set(GetRevokedKey(token), true, storedToken.ValidTo);
    }
}
