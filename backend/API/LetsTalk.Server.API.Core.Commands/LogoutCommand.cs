using MediatR;

namespace LetsTalk.Server.API.Core.Commands;

/// <param name="Token">The access token to revoke, if the request carried a valid one.</param>
/// <param name="RefreshToken">The refresh token whose session chain is ended, if the request carried one.</param>
public record LogoutCommand(string? Token, string? RefreshToken) : IRequest<Unit>;
