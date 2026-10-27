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

    private Mock<IAuthenticationClient> _authenticationClientMock;
    private LogoutCommandHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _authenticationClientMock = new Mock<IAuthenticationClient>();
        _handler = new LogoutCommandHandler(_authenticationClientMock.Object);
    }

    [Test]
    public async Task Handle_WhenCalled_ShouldRevokeTheToken()
    {
        // Act
        var result = await _handler.Handle(new LogoutCommand(Token), CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _authenticationClientMock.Verify(x => x.RevokeJwtTokenAsync(Token), Times.Once);
    }

    [Test]
    public async Task Handle_WhenRevocationFails_ShouldPropagateTheException()
    {
        // Arrange: the caller must know that the token is still valid
        _authenticationClientMock
            .Setup(x => x.RevokeJwtTokenAsync(Token))
            .ThrowsAsync(new InvalidOperationException("Authentication service error"));

        // Act
        var act = () => _handler.Handle(new LogoutCommand(Token), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
