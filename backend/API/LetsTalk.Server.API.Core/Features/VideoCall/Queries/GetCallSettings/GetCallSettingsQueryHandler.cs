using LetsTalk.Server.API.Core.Abstractions;
using LetsTalk.Server.Models.Dtos;
using LetsTalk.Server.Exceptions;
using MediatR;

namespace LetsTalk.Server.API.Core.Features.VideoCall.Queries.GetCallSettings;

public class GetCallSettingsQueryHandler(
    IIceServerConfigurationService iceServerConfigurationService) : IRequestHandler<GetCallSettingsQuery, CallSettingsDto>
{
    private readonly IIceServerConfigurationService _iceServerConfigurationService = iceServerConfigurationService;

    public async Task<CallSettingsDto> Handle(GetCallSettingsQuery _, CancellationToken cancellationToken)
    {
        var iceServerConfiguration = await _iceServerConfigurationService.GetIceServerConfigurationAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(iceServerConfiguration))
        {
            throw new BadRequestException("Invalid request");
        }

        return new CallSettingsDto
        {
            IceServerConfiguration = iceServerConfiguration
        };
    }
}
