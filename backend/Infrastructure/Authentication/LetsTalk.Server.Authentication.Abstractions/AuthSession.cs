namespace LetsTalk.Server.Authentication.Abstractions;

/// <param name="RefreshToken">Null when only a new access token was issued (the refresh token was reused within the grace period).</param>
/// <param name="RefreshExpiresAt">Null together with <paramref name="RefreshToken"/>.</param>
public record AuthSession(
    string AccessToken,
    DateTime AccessExpiresAt,
    string? RefreshToken,
    DateTime? RefreshExpiresAt);
