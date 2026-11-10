namespace LetsTalk.Server.API.Core.Commands;

/// <param name="RefreshToken">Null when only a new access token was issued (the refresh token had just been replaced).</param>
/// <param name="RefreshExpiresAt">Null together with <paramref name="RefreshToken"/>.</param>
public record SessionResult(
    string AccessToken,
    DateTime AccessExpiresAt,
    string? RefreshToken,
    DateTime? RefreshExpiresAt);
