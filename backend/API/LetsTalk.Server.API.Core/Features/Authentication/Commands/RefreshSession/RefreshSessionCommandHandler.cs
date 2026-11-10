using AutoMapper;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.Authentication.Abstractions;
using MediatR;

namespace LetsTalk.Server.API.Core.Features.Authentication.Commands.RefreshSession;

public class RefreshSessionCommandHandler(
    IAuthenticationClient authenticationClient,
    IMapper mapper) : IRequestHandler<RefreshSessionCommand, SessionResult?>
{
    private readonly IAuthenticationClient _authenticationClient = authenticationClient;
    private readonly IMapper _mapper = mapper;

    public async Task<SessionResult?> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _authenticationClient.RefreshSessionAsync(request.RefreshToken);

        return session is null ? null : _mapper.Map<SessionResult>(session);
    }
}
