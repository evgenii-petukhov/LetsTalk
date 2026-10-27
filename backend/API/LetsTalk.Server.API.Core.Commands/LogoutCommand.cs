using MediatR;

namespace LetsTalk.Server.API.Core.Commands;

public record LogoutCommand(string Token) : IRequest<Unit>;
