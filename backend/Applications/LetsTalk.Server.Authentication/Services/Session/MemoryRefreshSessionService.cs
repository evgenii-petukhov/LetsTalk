using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Authentication.Services.Session;

public class MemoryRefreshSessionService(
    IJwtCacheService jwtCacheService,
    IOptions<JwtSettings> jwtSettings,
    TimeProvider timeProvider,
    ILogger<MemoryRefreshSessionService> logger,
    IMemoryCache memoryCache) : RefreshSessionServiceBase(jwtCacheService, jwtSettings, timeProvider, logger)
{
    // The service is scoped but the cache is shared, so the lock that makes "mark as used" atomic is static
    private static readonly object UsedLock = new();

    private readonly IMemoryCache _memoryCache = memoryCache;

    protected override Task<RefreshRecord?> GetRecordAsync(string hash)
    {
        return Task.FromResult(_memoryCache.TryGetValue(GetRecordKey(hash), out RefreshRecord? record) ? record : null);
    }

    protected override Task SaveRecordAsync(string hash, RefreshRecord record, TimeSpan lifetime)
    {
        _memoryCache.Set(GetRecordKey(hash), record, lifetime);
        return Task.CompletedTask;
    }

    protected override Task DeleteRecordAsync(string hash)
    {
        _memoryCache.Remove(GetRecordKey(hash));
        return Task.CompletedTask;
    }

    protected override Task<DateTime?> TryMarkUsedAsync(string hash, DateTime now, TimeSpan lifetime)
    {
        lock (UsedLock)
        {
            if (_memoryCache.TryGetValue(GetUsedKey(hash), out DateTime firstUse))
            {
                return Task.FromResult<DateTime?>(firstUse);
            }

            _memoryCache.Set(GetUsedKey(hash), now, lifetime);
            return Task.FromResult<DateTime?>(null);
        }
    }

    protected override Task DeleteUsedMarkerAsync(string hash)
    {
        lock (UsedLock)
        {
            _memoryCache.Remove(GetUsedKey(hash));
        }

        return Task.CompletedTask;
    }

    protected override Task<string?> GetChainHeadAsync(string chainId)
    {
        return Task.FromResult(_memoryCache.TryGetValue(GetChainKey(chainId), out string? hash) ? hash : null);
    }

    protected override Task SetChainHeadAsync(string chainId, string hash, TimeSpan lifetime)
    {
        _memoryCache.Set(GetChainKey(chainId), hash, lifetime);
        return Task.CompletedTask;
    }

    protected override Task DeleteChainHeadAsync(string chainId)
    {
        _memoryCache.Remove(GetChainKey(chainId));
        return Task.CompletedTask;
    }
}
