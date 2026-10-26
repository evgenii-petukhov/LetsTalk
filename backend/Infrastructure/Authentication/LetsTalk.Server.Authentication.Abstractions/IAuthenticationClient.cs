namespace LetsTalk.Server.Authentication.Abstractions;

public interface IAuthenticationClient
{
    Task<(string token, DateTime expiresAt)> GenerateJwtTokenAsync(string accountId);

    Task<string?> ValidateJwtTokenAsync(string token);

    Task RevokeJwtTokenAsync(string token);

    Task<(int code, bool isCodeCreated, int ttl)> GenerateLoginCodeAsync(string email);

    Task<bool> ValidateLoginCodeAsync(string email, int code);
}
