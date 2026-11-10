using FluentAssertions;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.API.Core.Features.Authentication.Commands.Logout;
using LetsTalk.Server.Authentication.Abstractions;
using MediatR;
using Moq;

namespace LetsTalk.Server.API.Core.Tests.Handlers;

[TestFixture]
public class LogoutCommandHandlerTests
{
    private const string Token = "jwt-token";
    private const string RefreshToken = "refresh-token";

    private Mock<IAuthenticationClient> _authenticationClientMock;
    private LogoutCommandHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _authenticationClientMock = new Mock<IAuthenticationClient>();
        _handler = new LogoutCommandHandler(_authenticationClientMock.Object);
    }

    [Test]
    public async Task Handle_WhenCalled_ShouldRevokeTheTokenAndEndTheSession()
    {
        // Act
        var result = await _handler.Handle(new LogoutCommand(Token, RefreshToken), CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _authenticationClientMock.Verify(x => x.RevokeJwtTokenAsync(Token), Times.Once);
        _authenticationClientMock.Verify(x => x.EndSessionAsync(RefreshToken), Times.Once);
    }

    [Test]
    public async Task Handle_WhenOnlyAccessToken_ShouldRevokeItAndNotEndASession()
    {
        // Act
        await _handler.Handle(new LogoutCommand(Token, null), CancellationToken.None);

        // Assert
        _authenticationClientMock.Verify(x => x.RevokeJwtTokenAsync(Token), Times.Once);
        _authenticationClientMock.Verify(x => x.EndSessionAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenOnlyRefreshToken_ShouldEndTheSessionAndNotRevokeAToken()
    {
        // Arrange: the access token has already expired, but the chain must still be ended
        // Act
        await _handler.Handle(new LogoutCommand(null, RefreshToken), CancellationToken.None);

        // Assert
        _authenticationClientMock.Verify(x => x.EndSessionAsync(RefreshToken), Times.Once);
        _authenticationClientMock.Verify(x => x.RevokeJwtTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhenRevocationFails_ShouldPropagateTheException()
    {
        // Arrange: the caller must know that the token is still valid
        _authenticationClientMock
            .Setup(x => x.RevokeJwtTokenAsync(Token))
            .ThrowsAsync(new InvalidOperationException("Authentication service error"));

        // Act
        var act = () => _handler.Handle(new LogoutCommand(Token, RefreshToken), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Handle_WhenEndingTheSessionFails_ShouldPropagateTheException()
    {
        // Arrange
        _authenticationClientMock
            .Setup(x => x.EndSessionAsync(RefreshToken))
            .ThrowsAsync(new InvalidOperationException("Authentication service error"));

        // Act
        var act = () => _handler.Handle(new LogoutCommand(Token, RefreshToken), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
