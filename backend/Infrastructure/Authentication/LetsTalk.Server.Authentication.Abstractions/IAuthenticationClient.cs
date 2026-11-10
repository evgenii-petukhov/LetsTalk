namespace LetsTalk.Server.Authentication.Abstractions;

public interface IAuthenticationClient
{
    Task<(string token, DateTime expiresAt)> GenerateJwtTokenAsync(string accountId);

    Task<string?> ValidateJwtTokenAsync(string token);

    Task RevokeJwtTokenAsync(string token);

    Task<(string token, DateTime expiresAt)> GenerateBackgroundTokenAsync(string accountId);

    Task<AuthSession> CreateSessionAsync(string accountId);

    /// <returns>Null when the refresh token is rejected. An outage of the Authentication service throws instead.</returns>
    Task<AuthSession?> RefreshSessionAsync(string refreshToken);

    Task EndSessionAsync(string refreshToken);

    Task<(int code, bool isCodeCreated, int ttl)> GenerateLoginCodeAsync(string email);

    Task<bool> ValidateLoginCodeAsync(string email, int code);
}
