using LetsTalk.Server.Models.Dtos;
using AutoMapper;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Exceptions;
using LetsTalk.Server.Persistence.AgnosticServices.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using MediatR;
using LetsTalk.Server.API.Core.Commands;

namespace LetsTalk.Server.API.Core.Features.Authentication.Commands.EmailLogin;

public class EmailLoginCommandHandler(
    IAuthenticationClient authenticationClient,
    IAccountAgnosticService accountAgnosticService,
    IMapper mapper) : IRequestHandler<EmailLoginCommand, SessionDto>
{
    private readonly IAuthenticationClient _authenticationClient = authenticationClient;
    private readonly IAccountAgnosticService _accountAgnosticService = accountAgnosticService;
    private readonly IMapper _mapper = mapper;

    public async Task<SessionDto> Handle(EmailLoginCommand request, CancellationToken cancellationToken)
    {
        var validator = new EmailLoginCommandValidator(_authenticationClient);
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Invalid request", validationResult);
        }

        var accountId = await _accountAgnosticService.GetOrCreateAsync(
            AccountTypes.Email,
            request.Email?.Trim().ToLowerInvariant()!,
            cancellationToken);

        var session = await _authenticationClient.CreateSessionAsync(accountId);

        return _mapper.Map<SessionDto>(session);
    }
}
