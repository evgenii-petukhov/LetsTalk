using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.API.Controllers;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.API.Models.Login;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.DateHelpers;
using LetsTalk.Server.Exceptions;
using LetsTalk.Server.Models.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.API.Tests;

[TestFixture]
public class AuthenticationControllerTests
{
    private const string Token = "jwt-token";
    private const string Email = "user@example.com";
    private const int Code = 1234;
    private const string CookieName = "auth-token";
    private const int AntiSpamLifetimeInSeconds = 60;
    private const string ExpiredCookieDate = "expires=Thu, 01 Jan 1970";

    private static readonly DateTime ExpiresAt = DateTime.UtcNow.AddDays(30);

    private Mock<IMediator> _mediatorMock;
    private Mock<IMapper> _mapperMock;
    private DefaultHttpContext _httpContext;
    private AuthenticationController _controller;

    [SetUp]
    public void SetUp()
    {
        _mediatorMock = new Mock<IMediator>();
        _mapperMock = new Mock<IMapper>();
        _httpContext = new DefaultHttpContext();

        _mapperMock
            .Setup(x => x.Map<EmailLoginCommand>(It.IsAny<object>()))
            .Returns(new EmailLoginCommand { Email = Email, Code = Code });

        _controller = new AuthenticationController(
            _mediatorMock.Object,
            _mapperMock.Object,
            Options.Create(new SecuritySettings { AntiSpamTokenLifeTimeInSeconds = AntiSpamLifetimeInSeconds }),
            Options.Create(new AuthCookieSettings()))
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    [Test]
    public async Task EmailLoginAsync_WhenSuccess_ShouldSetHttpOnlyCookieAndNotReturnToken()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EmailLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailLoginResult(Token, ExpiresAt));

        // Act
        var result = await _controller.EmailLoginAsync(CreateRequest(), CancellationToken.None);

        // Assert
        var dto = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<LoginResponseDto>().Subject;
        dto.Success.Should().BeTrue();

        var setCookie = _httpContext.Response.Headers.SetCookie.ToString();
        setCookie.Should().StartWith($"{CookieName}={Token}");
        setCookie.ToLowerInvariant().Should()
            .Contain("httponly")
            .And.Contain("secure")
            .And.Contain("samesite=strict")
            .And.Contain("path=/")
            .And.Contain("expires=");
        setCookie.ToLowerInvariant().Should().NotContain("domain=");
    }

    [Test]
    public async Task EmailLoginAsync_WhenCodeWrong_ShouldNotSetCookie()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EmailLoginCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BadRequestException("Invalid request"));

        // Act
        var act = () => _controller.EmailLoginAsync(CreateRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _httpContext.Response.Headers.SetCookie.Should().BeEmpty();
    }

    [Test]
    public async Task LogoutAsync_WhenCredentialValid_ShouldRevokeAndClearCookie()
    {
        // Arrange
        _httpContext.Items["Token"] = Token;

        // Act
        var result = await _controller.LogoutAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(
            x => x.Send(It.Is<LogoutCommand>(c => c.Token == Token), It.IsAny<CancellationToken>()),
            Times.Once);
        _httpContext.Response.Headers.SetCookie.ToString().Should()
            .Contain($"{CookieName}=;").And.Contain(ExpiredCookieDate);
    }

    [Test]
    public async Task LogoutAsync_WhenNoCredential_ShouldReturnNoContentAndClearCookie()
    {
        // Act
        var result = await _controller.LogoutAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _httpContext.Response.Headers.SetCookie.ToString().Should()
            .Contain($"{CookieName}=;").And.Contain(ExpiredCookieDate);
    }

    [Test]
    public async Task LogoutAsync_WhenCookieStale_ShouldReturnNoContent()
    {
        // Arrange: JwtMiddleware found the cookie invalid, so no token was stored
        _httpContext.Request.Headers.Cookie = $"{CookieName}=stale";

        // Act
        var result = await _controller.LogoutAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static EmailLoginRequest CreateRequest() => new()
    {
        Email = Email,
        Code = Code,
        AntiSpamToken = DateHelper.GetUnixTimestamp()
    };
}
