using LetsTalk.Server.Authentication.Models;

namespace LetsTalk.Server.Authentication.Abstractions;

public interface IJwtCacheService
{
    Task<string?> GetAccountIdAsync(string? token);

    ValueTask<StoredToken> GenerateAsync(string accountId, TimeSpan lifetime);

    Task RevokeAsync(string token);
}
