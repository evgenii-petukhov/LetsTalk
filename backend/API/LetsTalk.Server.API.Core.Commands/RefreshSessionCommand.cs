using MediatR;

namespace LetsTalk.Server.API.Core.Commands;

/// <summary>The result is null when the refresh token is rejected.</summary>
public record RefreshSessionCommand(string RefreshToken) : IRequest<SessionResult?>;
