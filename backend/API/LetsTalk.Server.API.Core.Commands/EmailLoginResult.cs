namespace LetsTalk.Server.API.Core.Commands;

public record EmailLoginResult(string Token, DateTime ExpiresAt);
