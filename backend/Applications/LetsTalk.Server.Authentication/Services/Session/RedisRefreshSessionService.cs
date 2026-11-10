using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Persistence.Redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;

namespace LetsTalk.Server.Authentication.Services.Session;

public class RedisRefreshSessionService(
    IJwtCacheService jwtCacheService,
    IOptions<JwtSettings> jwtSettings,
    TimeProvider timeProvider,
    ILogger<RedisRefreshSessionService> logger,
    RedisConnection redisConnection) : RefreshSessionServiceBase(jwtCacheService, jwtSettings, timeProvider, logger)
{
    private readonly IDatabase _database = redisConnection.Connection.GetDatabase();

    protected override async Task<RefreshRecord?> GetRecordAsync(string hash)
    {
        var value = await _database.StringGetAsync(new RedisKey(GetRecordKey(hash)));
        return value == RedisValue.Null ? null : JsonSerializer.Deserialize<RefreshRecord>(value.ToString());
    }

    protected override async Task SaveRecordAsync(string hash, RefreshRecord record, TimeSpan lifetime)
    {
        await _database.StringSetAsync(
            new RedisKey(GetRecordKey(hash)),
            new RedisValue(JsonSerializer.Serialize(record)),
            lifetime);
    }

    protected override async Task DeleteRecordAsync(string hash)
    {
        await _database.KeyDeleteAsync(new RedisKey(GetRecordKey(hash)));
    }

    // SET ... NX is atomic: of several simultaneous rotations exactly one stores the marker and wins
    protected override async Task<DateTime?> TryMarkUsedAsync(string hash, DateTime now, TimeSpan lifetime)
    {
        var key = new RedisKey(GetUsedKey(hash));
        var marked = await _database.StringSetAsync(key, new RedisValue(now.Ticks.ToString(CultureInfo.InvariantCulture)), lifetime, When.NotExists);
        if (marked)
        {
            return null;
        }

        // The marker can expire or be evicted between the two commands. Another renewal has just claimed the
        // token, so read it as used just now (inside the grace period), not as an old replay that ends the chain.
        var firstUse = await _database.StringGetAsync(key);
        return firstUse == RedisValue.Null
            ? now
            : new DateTime(long.Parse(firstUse.ToString(), CultureInfo.InvariantCulture), DateTimeKind.Utc);
    }

    protected override async Task DeleteUsedMarkerAsync(string hash)
    {
        await _database.KeyDeleteAsync(new RedisKey(GetUsedKey(hash)));
    }

    protected override async Task<string?> GetChainHeadAsync(string chainId)
    {
        var value = await _database.StringGetAsync(new RedisKey(GetChainKey(chainId)));
        return value == RedisValue.Null ? null : value.ToString();
    }

    protected override async Task SetChainHeadAsync(string chainId, string hash, TimeSpan lifetime)
    {
        await _database.StringSetAsync(new RedisKey(GetChainKey(chainId)), new RedisValue(hash), lifetime);
    }

    protected override async Task DeleteChainHeadAsync(string chainId)
    {
        await _database.KeyDeleteAsync(new RedisKey(GetChainKey(chainId)));
    }
}
