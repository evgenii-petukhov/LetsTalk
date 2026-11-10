namespace LetsTalk.Server.Authentication.Models;

// What the store keeps for one refresh token. It is addressed by the hash of the token, never by the token itself.
public class RefreshRecord
{
    public required string AccountId { get; init; }

    public required string ChainId { get; init; }

    public DateTime ChainStartedAt { get; init; }

    public DateTime ExpiresAt { get; init; }
}
