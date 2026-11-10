using LetsTalk.Server.Authentication.Models;

namespace LetsTalk.Server.Authentication.Abstractions;

public interface IRefreshSessionService
{
    Task<SessionTokens> IssueAsync(string accountId);

    /// <returns>Null when the refresh token is unknown, expired, belongs to an ended chain or was reused after the grace period.</returns>
    Task<SessionTokens?> RotateAsync(string refreshToken);

    Task EndByRefreshTokenAsync(string refreshToken);
}
