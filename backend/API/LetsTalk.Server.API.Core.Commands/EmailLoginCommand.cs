using MediatR;

namespace LetsTalk.Server.API.Core.Commands;

public class EmailLoginCommand : IRequest<EmailLoginResult>
{
    public string? Email { get; set; }

    public int Code { get; set; }
}
