using FluentAssertions;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.API.Core.Features.Authentication.Commands.RefreshSession;
using LetsTalk.Server.Authentication.Abstractions;
using Moq;

namespace LetsTalk.Server.API.Core.Tests.Handlers;

[TestFixture]
public class RefreshSessionCommandHandlerTests
{
    private const string RefreshToken = "refresh-token";
    private const string NewRefreshToken = "new-refresh-token";
    private const string AccessToken = "access-token";

    private static readonly DateTime AccessExpiresAt = new(2030, 1, 1, 0, 15, 0, DateTimeKind.Utc);
    private static readonly DateTime RefreshExpiresAt = new(2030, 1, 31, 0, 0, 0, DateTimeKind.Utc);

    private Mock<IAuthenticationClient> _authenticationClientMock;
    private RefreshSessionCommandHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _authenticationClientMock = new Mock<IAuthenticationClient>();
        _handler = new RefreshSessionCommandHandler(_authenticationClientMock.Object);
    }

    [Test]
    public async Task Handle_WhenRefreshTokenAccepted_ShouldReturnBothTokensAndTheirExpiry()
    {
        // Arrange
        _authenticationClientMock
            .Setup(x => x.RefreshSessionAsync(RefreshToken))
            .ReturnsAsync(new AuthSession(AccessToken, AccessExpiresAt, NewRefreshToken, RefreshExpiresAt));

        // Act
        var result = await _handler.Handle(new RefreshSessionCommand(RefreshToken), CancellationToken.None);

        // Assert
        result.Should().Be(new SessionResult(AccessToken, AccessExpiresAt, NewRefreshToken, RefreshExpiresAt));
    }

    [Test]
    public async Task Handle_WhenRefreshTokenReusedWithinGrace_ShouldReturnTheAccessTokenOnly()
    {
        // Arrange
        _authenticationClientMock
            .Setup(x => x.RefreshSessionAsync(RefreshToken))
            .ReturnsAsync(new AuthSession(AccessToken, AccessExpiresAt, null, null));

        // Act
        var result = await _handler.Handle(new RefreshSessionCommand(RefreshToken), CancellationToken.None);

        // Assert
        result.Should().Be(new SessionResult(AccessToken, AccessExpiresAt, null, null));
    }

    [Test]
    public async Task Handle_WhenRefreshTokenRejected_ShouldReturnNull()
    {
        // Arrange
        _authenticationClientMock
            .Setup(x => x.RefreshSessionAsync(RefreshToken))
            .ReturnsAsync((AuthSession?)null);

        // Act
        var result = await _handler.Handle(new RefreshSessionCommand(RefreshToken), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Handle_WhenAuthenticationServiceFails_ShouldPropagateTheException()
    {
        // Arrange: an outage must not look like a rejected token
        _authenticationClientMock
            .Setup(x => x.RefreshSessionAsync(RefreshToken))
            .ThrowsAsync(new InvalidOperationException("Authentication service error"));

        // Act
        var act = () => _handler.Handle(new RefreshSessionCommand(RefreshToken), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
