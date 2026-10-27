using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Notifications.Abstractions;
using LetsTalk.Server.Notifications.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.Notifications.Tests;

[TestFixture]
public class NotificationHubTests
{
    private const string CookieName = "auth-token";
    private const string CookieToken = "cookie-token";
    private const string ExplicitToken = "explicit-token";
    private const string AccountId = "42";
    private const string ConnectionId = "connection-1";

    private Mock<IConnectionManager> _connectionManagerMock;
    private Mock<IAuthenticationClient> _authenticationClientMock;
    private Mock<HubCallerContext> _contextMock;
    private DefaultHttpContext _httpContext;
    private NotificationHub _hub;

    [SetUp]
    public void SetUp()
    {
        _connectionManagerMock = new Mock<IConnectionManager>();
        _authenticationClientMock = new Mock<IAuthenticationClient>();
        _httpContext = new DefaultHttpContext();

        var features = new FeatureCollection();
        features.Set<IHttpContextFeature>(new HttpContextFeature { HttpContext = _httpContext });
        _contextMock = new Mock<HubCallerContext>();
        _contextMock.SetupGet(x => x.ConnectionId).Returns(ConnectionId);
        _contextMock.SetupGet(x => x.Features).Returns(features);

        _hub = new NotificationHub(
            _connectionManagerMock.Object,
            _authenticationClientMock.Object,
            Options.Create(new AuthCookieSettings()))
        {
            Context = _contextMock.Object
        };
    }

    [TearDown]
    public void TearDown()
    {
        _hub.Dispose();
    }

    [Test]
    public async Task OnConnectedAsync_WhenCookieValid_ShouldRegisterConnection()
    {
        // Arrange
        _httpContext.Request.Headers.Cookie = $"{CookieName}={CookieToken}";
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ReturnsAsync(AccountId);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _connectionManagerMock.Verify(x => x.AddConnectionId(AccountId, ConnectionId), Times.Once);
        _contextMock.Verify(x => x.Abort(), Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_WhenCookieMissing_ShouldNotRegisterAndNotAbort()
    {
        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _connectionManagerMock.Verify(x => x.AddConnectionId(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _contextMock.Verify(x => x.Abort(), Times.Never);
        _authenticationClientMock.Verify(x => x.ValidateJwtTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_WhenCookieInvalid_ShouldAbortAndNotRegister()
    {
        // Arrange
        _httpContext.Request.Headers.Cookie = $"{CookieName}={CookieToken}";

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _connectionManagerMock.Verify(x => x.AddConnectionId(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _contextMock.Verify(x => x.Abort(), Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_WhenValidationThrowsRpcException_ShouldAbortAndNotRegister()
    {
        // Arrange
        _httpContext.Request.Headers.Cookie = $"{CookieName}={CookieToken}";
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ThrowsAsync(new RpcException(new Status(StatusCode.Unknown, "Invalid token")));

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _connectionManagerMock.Verify(x => x.AddConnectionId(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _contextMock.Verify(x => x.Abort(), Times.Once);
    }

    [Test]
    public async Task AuthorizeAsync_WhenTokenValid_ShouldRegisterConnection()
    {
        // Arrange
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(ExplicitToken))
            .ReturnsAsync(AccountId);

        // Act
        await _hub.AuthorizeAsync(ExplicitToken);

        // Assert
        _connectionManagerMock.Verify(x => x.AddConnectionId(AccountId, ConnectionId), Times.Once);
    }

    [Test]
    public async Task AuthorizeAsync_WhenTokenInvalid_ShouldNotRegisterConnection()
    {
        // Act
        await _hub.AuthorizeAsync(ExplicitToken);

        // Assert
        _connectionManagerMock.Verify(x => x.AddConnectionId(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private sealed class HttpContextFeature : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; }
    }
}
