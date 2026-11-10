using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace LetsTalk.Server.Authentication.Services.Session;

public abstract class RefreshSessionServiceBase(
    IJwtCacheService jwtCacheService,
    IOptions<JwtSettings> jwtSettings,
    TimeProvider timeProvider,
    ILogger logger) : IRefreshSessionService
{
    private const int RefreshTokenBytes = 32;

    private static readonly Action<ILogger, string, string, Exception?> _logChainEnded =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(0, nameof(RefreshSessionServiceBase)),
            "A refresh token that was already replaced was presented, the session chain {ChainId} of account {AccountId} was ended");

    private readonly IJwtCacheService _jwtCacheService = jwtCacheService;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger _logger = logger;

    public Task<SessionTokens> IssueAsync(string accountId)
    {
        var now = Now();
        return IssueTokensAsync(accountId, Guid.NewGuid().ToString("N"), chainStartedAt: now, now);
    }

    public async Task<SessionTokens?> RotateAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var record = await GetRecordAsync(hash);
        var now = Now();

        // The record's expiry is already capped by the absolute lifetime of the chain
        if (record == null
            || record.ExpiresAt <= now
            || await GetChainHeadAsync(record.ChainId) == null)
        {
            return null;
        }

        var firstUse = await TryMarkUsedAsync(hash, now, record.ExpiresAt - now);
        if (firstUse == null)
        {
            return await IssueTokensAsync(record.AccountId, record.ChainId, record.ChainStartedAt, now);
        }

        if (now - firstUse.Value <= _jwtSettings.RefreshReuseGrace)
        {
            // Another tab renewed a moment ago and already holds the successor; only the short credential is needed
            var access = await _jwtCacheService.GenerateAsync(record.AccountId, _jwtSettings.AccessTokenLifetime);
            return new SessionTokens { AccessToken = access.Token!, AccessExpiresAt = access.ValidTo };
        }

        await EndChainAsync(record.ChainId);
        _logChainEnded(_logger, record.ChainId, record.AccountId, null);
        return null;
    }

    public async Task EndByRefreshTokenAsync(string refreshToken)
    {
        var record = await GetRecordAsync(Hash(refreshToken));
        if (record != null)
        {
            await EndChainAsync(record.ChainId);
        }
    }

    protected abstract Task<RefreshRecord?> GetRecordAsync(string hash);

    protected abstract Task SaveRecordAsync(string hash, RefreshRecord record, TimeSpan lifetime);

    protected abstract Task DeleteRecordAsync(string hash);

    /// <returns>Null when this call marked the token as used, otherwise the time of the first use. Must be atomic.</returns>
    protected abstract Task<DateTime?> TryMarkUsedAsync(string hash, DateTime now, TimeSpan lifetime);

    protected abstract Task<string?> GetChainHeadAsync(string chainId);

    protected abstract Task SetChainHeadAsync(string chainId, string hash, TimeSpan lifetime);

    protected abstract Task DeleteChainHeadAsync(string chainId);

    private async Task<SessionTokens> IssueTokensAsync(string accountId, string chainId, DateTime chainStartedAt, DateTime now)
    {
        var expiresAt = Min(now + _jwtSettings.RefreshIdleLifetime, chainStartedAt + _jwtSettings.RefreshAbsoluteLifetime);
        var lifetime = expiresAt - now;
        var refreshToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        var hash = Hash(refreshToken);

        await SaveRecordAsync(
            hash,
            new RefreshRecord
            {
                AccountId = accountId,
                ChainId = chainId,
                ChainStartedAt = chainStartedAt,
                ExpiresAt = expiresAt
            },
            lifetime);
        await SetChainHeadAsync(chainId, hash, lifetime);

        var access = await _jwtCacheService.GenerateAsync(accountId, _jwtSettings.AccessTokenLifetime);
        return new SessionTokens
        {
            AccessToken = access.Token!,
            AccessExpiresAt = access.ValidTo,
            RefreshToken = refreshToken,
            RefreshExpiresAt = expiresAt
        };
    }

    private async Task EndChainAsync(string chainId)
    {
        var head = await GetChainHeadAsync(chainId);
        if (head != null)
        {
            await DeleteRecordAsync(head);
        }

        await DeleteChainHeadAsync(chainId);
    }

    // The clock is injected (TimeProvider.System in production) so that tests can move time forward and check the
    // grace, idle and absolute lifetimes without waiting for them
    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;

    private static DateTime Min(DateTime first, DateTime second) => first < second ? first : second;

    private static string Hash(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    protected static string GetRecordKey(string hash) => $"refresh:{hash}";

    protected static string GetUsedKey(string hash) => $"refresh-used:{hash}";

    protected static string GetChainKey(string chainId) => $"refresh-chain:{chainId}";
}
