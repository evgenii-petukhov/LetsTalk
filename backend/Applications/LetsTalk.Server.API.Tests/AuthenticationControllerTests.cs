using AutoMapper;
using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.API.Controllers;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.API.Middleware.Exceptions;
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
    private const string NewToken = "new-jwt-token";
    private const string RefreshToken = "refresh-token";
    private const string NewRefreshToken = "new-refresh-token";
    private const string Email = "user@example.com";
    private const int Code = 1234;
    private const string CookieName = "auth-token";
    private const string RefreshCookieName = "auth-refresh";
    private const string RefreshCookiePath = "path=/api/authentication";
    private const int AntiSpamLifetimeInSeconds = 60;
    private const string ExpiredCookieDate = "expires=thu, 01 jan 1970";

    private static readonly DateTime AccessExpiresAt = DateTime.UtcNow.AddMinutes(15);
    private static readonly DateTime RefreshExpiresAt = DateTime.UtcNow.AddDays(30);

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
    public async Task EmailLoginAsync_WhenSuccess_ShouldSetAccessAndRefreshCookies()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EmailLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionResult(Token, AccessExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        var result = await _controller.EmailLoginAsync(CreateRequest(), CancellationToken.None);

        // Assert
        var dto = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<LoginResponseDto>().Subject;
        dto.Success.Should().BeTrue();

        var access = GetSetCookie(CookieName);
        access.Should().StartWith($"{CookieName}={Token}");
        access.Should()
            .Contain("httponly")
            .And.Contain("secure")
            .And.Contain("samesite=strict")
            .And.Contain("path=/")
            .And.Contain("expires=");
        access.Should().NotContain("domain=").And.NotContain("path=/api");

        // The refresh token reaches only the authentication endpoints
        var refresh = GetSetCookie(RefreshCookieName);
        refresh.Should().StartWith($"{RefreshCookieName}={RefreshToken}");
        refresh.Should()
            .Contain("httponly")
            .And.Contain("secure")
            .And.Contain("samesite=strict")
            .And.Contain(RefreshCookiePath)
            .And.Contain("expires=");
    }

    [Test]
    public async Task EmailLoginAsync_WhenSuccess_ShouldNotReturnAnyToken()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EmailLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionResult(Token, AccessExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        var result = await _controller.EmailLoginAsync(CreateRequest(), CancellationToken.None);

        // Assert
        var body = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value;
        System.Text.Json.JsonSerializer.Serialize(body).Should().NotContain(Token).And.NotContain(RefreshToken);
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
    public async Task RefreshAsync_WhenValid_ShouldSetNewCookiesAndReturnNoContent()
    {
        // Arrange
        _httpContext.Request.Headers.Cookie = $"{RefreshCookieName}={RefreshToken}";
        _mediatorMock
            .Setup(x => x.Send(It.Is<RefreshSessionCommand>(c => c.RefreshToken == RefreshToken), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionResult(NewToken, AccessExpiresAt, NewRefreshToken, RefreshExpiresAt));

        // Act
        var result = await _controller.RefreshAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        GetSetCookie(CookieName).Should().StartWith($"{CookieName}={NewToken}").And.Contain("httponly");
        GetSetCookie(RefreshCookieName).Should()
            .StartWith($"{RefreshCookieName}={NewRefreshToken}")
            .And.Contain("httponly")
            .And.Contain(RefreshCookiePath);
    }

    [Test]
    public async Task RefreshAsync_WhenOnlyAccessTokenIssued_ShouldKeepTheRefreshCookie()
    {
        // Arrange: the refresh token was replaced a moment ago by another tab, which already stored its successor
        _httpContext.Request.Headers.Cookie = $"{RefreshCookieName}={RefreshToken}";
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionResult(NewToken, AccessExpiresAt, null, null));

        // Act
        var result = await _controller.RefreshAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        GetSetCookies().Should().ContainSingle().Which.Should().StartWith($"{CookieName}={NewToken}");
    }

    [Test]
    public async Task RefreshAsync_WhenInvalid_ShouldClearCookiesAndReturn401()
    {
        // Arrange
        _httpContext.Request.Headers.Cookie = $"{RefreshCookieName}={RefreshToken}";
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionResult?)null);

        // Act
        var result = await _controller.RefreshAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        AssertBothCookiesCleared();
    }

    [Test]
    public async Task RefreshAsync_WhenNoRefreshCookie_ShouldClearCookiesAndReturn401WithoutAskingTheService()
    {
        // Act
        var result = await _controller.RefreshAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertBothCookiesCleared();
    }

    [Test]
    public async Task RefreshAsync_WhenOnlyAuthorizationHeader_ShouldReturn401()
    {
        // Arrange: an access token in the header is no reason to issue a new session
        _httpContext.Request.Headers.Authorization = $"Bearer {Token}";
        _httpContext.Items["Token"] = Token;

        // Act
        var result = await _controller.RefreshAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(StatusCode.Unavailable)]
    [TestCase(StatusCode.DeadlineExceeded)]
    public async Task RefreshAsync_WhenAuthenticationServiceUnavailable_ShouldThrowServiceUnavailableAndKeepCookies(StatusCode statusCode)
    {
        // Arrange: an outage is not a sign-out
        _httpContext.Request.Headers.Cookie = $"{RefreshCookieName}={RefreshToken}";
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RpcException(new Status(statusCode, "down")));

        // Act
        var act = () => _controller.RefreshAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ServiceUnavailableException>();
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
            x => x.Send(It.Is<LogoutCommand>(c => c.Token == Token && c.RefreshToken == null), It.IsAny<CancellationToken>()),
            Times.Once);
        GetSetCookie(CookieName).Should().Contain($"{CookieName}=;").And.Contain(ExpiredCookieDate);
    }

    [Test]
    public async Task LogoutAsync_ShouldEndSessionAndClearBothCookies()
    {
        // Arrange
        _httpContext.Items["Token"] = Token;
        _httpContext.Request.Headers.Cookie = $"{RefreshCookieName}={RefreshToken}";

        // Act
        var result = await _controller.LogoutAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(
            x => x.Send(It.Is<LogoutCommand>(c => c.Token == Token && c.RefreshToken == RefreshToken), It.IsAny<CancellationToken>()),
            Times.Once);
        AssertBothCookiesCleared();
    }

    [Test]
    public async Task LogoutAsync_WhenOnlyRefreshCookie_ShouldStillEndTheSession()
    {
        // Arrange: the access token has expired, but signing out must still end the refresh chain
        _httpContext.Request.Headers.Cookie = $"{RefreshCookieName}={RefreshToken}";

        // Act
        var result = await _controller.LogoutAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(
            x => x.Send(It.Is<LogoutCommand>(c => c.Token == null && c.RefreshToken == RefreshToken), It.IsAny<CancellationToken>()),
            Times.Once);
        AssertBothCookiesCleared();
    }

    [Test]
    public async Task LogoutAsync_WhenNoCredential_ShouldReturnNoContentAndClearBothCookies()
    {
        // Act
        var result = await _controller.LogoutAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(x => x.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertBothCookiesCleared();
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

    private string[] GetSetCookies() =>
        _httpContext.Response.Headers.SetCookie.Select(x => x!.ToLowerInvariant()).ToArray();

    private string GetSetCookie(string name) =>
        GetSetCookies().Single(x => x.StartsWith($"{name}="));

    private void AssertBothCookiesCleared()
    {
        GetSetCookie(CookieName).Should().Contain($"{CookieName}=;").And.Contain(ExpiredCookieDate);
        GetSetCookie(RefreshCookieName).Should()
            .Contain($"{RefreshCookieName}=;")
            .And.Contain(ExpiredCookieDate)
            .And.Contain(RefreshCookiePath);
    }
}
