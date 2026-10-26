using AutoMapper;
using LetsTalk.Server.API.Models.Login;
using LetsTalk.Server.Models.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using LetsTalk.Server.Exceptions;
using LetsTalk.Server.API.Validation;
using Microsoft.Extensions.Options;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.API.Middleware;
using LetsTalk.Server.Authentication.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace LetsTalk.Server.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController(
    IMediator mediator,
    IMapper mapper,
    IOptions<SecuritySettings> securitySettingsOptions,
    IAuthenticationClient authenticationClient,
    IOptions<AuthCookieSettings> authCookieSettingsOptions) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IMapper _mapper = mapper;
    private readonly SecuritySettings _securitySettings = securitySettingsOptions.Value;
    private readonly IAuthenticationClient _authenticationClient = authenticationClient;
    private readonly AuthCookieSettings _authCookieSettings = authCookieSettingsOptions.Value;

    [HttpPost("email-login")]
    public async Task<ActionResult<LoginResponseDto>> EmailLoginAsync(EmailLoginRequest request, CancellationToken cancellationToken)
    {
        var validator = new EmailLoginRequestValidator(_securitySettings);
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Invalid request", validationResult);
        }

        var result = await _mediator.Send(_mapper.Map<EmailLoginCommand>(request), cancellationToken);

        Response.AppendAuthCookie(_authCookieSettings, result.Token, result.ExpiresAt);

        return Ok(new LoginResponseDto { Success = true });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        // JwtMiddleware stores the token only when the session cookie or the header was valid
        if (HttpContext.Items["Token"] is string token)
        {
            await _authenticationClient.RevokeJwtTokenAsync(token);
        }

        Response.DeleteAuthCookie(_authCookieSettings);

        return NoContent();
    }

    [HttpPost("generate-login-code")]
    public async Task<ActionResult<GenerateLoginCodeResponseDto>> GenerateLoginCodeAsync(GenerateLoginCodeRequest request, CancellationToken cancellationToken)
    {
        var validator = new GenerateLoginCodeRequestValidator(_securitySettings);
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Invalid request", validationResult);
        }

        var result = await _mediator.Send(new GenerateLoginCodeCommand(request.Email!), cancellationToken);

        return Ok(result);
    }
}
