using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.Authentication.Abstractions;
using MediatR;

namespace LetsTalk.Server.API.Core.Features.Authentication.Commands.Logout;

public class LogoutCommandHandler(IAuthenticationClient authenticationClient) : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IAuthenticationClient _authenticationClient = authenticationClient;

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _authenticationClient.RevokeJwtTokenAsync(request.Token);

        return Unit.Value;
    }
}
