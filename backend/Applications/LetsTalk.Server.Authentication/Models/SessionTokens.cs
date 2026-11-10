namespace LetsTalk.Server.Authentication.Models;

public class SessionTokens
{
    public required string AccessToken { get; init; }

    public DateTime AccessExpiresAt { get; init; }

    /// <summary>Null when only a new access token was issued (a refresh token reused within the grace period).</summary>
    public string? RefreshToken { get; init; }

    public DateTime? RefreshExpiresAt { get; init; }
}
