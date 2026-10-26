using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Exceptions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Persistence.Redis;
using StackExchange.Redis;
using System.Text.Json;

namespace LetsTalk.Server.Authentication.Services.Cache.Token;

public class RedisCacheTokenService(
    IJwtStorageService jwtStorageService,
    RedisConnection redisConnection) : CacheTokenServiceBase, IJwtCacheService
{
    private readonly IJwtStorageService _jwtStorageService = jwtStorageService;
    private readonly IDatabase _database = redisConnection.Connection.GetDatabase();

    public async Task<string?> GetAccountIdAsync(string? token)
    {
        if (token == null)
        {
            return null;
        }

        if (await _database.KeyExistsAsync(new RedisKey(GetRevokedKey(token))))
        {
            return null;
        }

        var key = new RedisKey(GetTokenKey(token!));

        var cachedAccountId = await _database.StringGetAsync(key);

        if (cachedAccountId == RedisValue.Null)
        {
            var storedToken = await _jwtStorageService.GetStoredTokenAsync(token);

            await _database.StringSetAsync(
                key,
                new RedisValue(JsonSerializer.Serialize(storedToken!.AccountId)),
                storedToken!.ValidTo - DateTime.Now,
            When.NotExists);

            await _database.KeyExpireAsync(key, storedToken.ValidTo);

            return storedToken.AccountId;
        }

        return JsonSerializer.Deserialize<string?>(cachedAccountId!.ToString())!;
    }

    public async ValueTask<string> GenerateAsync(string accountId)
    {
        var storedToken = _jwtStorageService.Generate(accountId);

        var key = new RedisKey(GetTokenKey(storedToken.Token!));

        await _database.StringSetAsync(
                key,
                new RedisValue(JsonSerializer.Serialize(storedToken.AccountId)),
                storedToken.ValidTo - DateTime.Now);

        return storedToken.Token!;
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

        var remainingLifetime = storedToken?.ValidTo - DateTime.UtcNow;
        if (remainingLifetime == null || remainingLifetime <= TimeSpan.Zero)
        {
            return;
        }

        await _database.StringSetAsync(
            new RedisKey(GetRevokedKey(token)),
            new RedisValue("1"),
            remainingLifetime.Value);
        await _database.KeyDeleteAsync(new RedisKey(GetTokenKey(token)));
    }
}
