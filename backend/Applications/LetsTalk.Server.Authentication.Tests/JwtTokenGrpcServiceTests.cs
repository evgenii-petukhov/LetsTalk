using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Models;
using Moq;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class JwtTokenGrpcServiceTests
{
    private const string Token = "jwt-token";
    private const string AccountId = "42";

    private Mock<IJwtCacheService> _jwtCacheServiceMock;
    private Services.JwtTokenGrpcService _service;

    [SetUp]
    public void SetUp()
    {
        _jwtCacheServiceMock = new Mock<IJwtCacheService>();
        _service = new Services.JwtTokenGrpcService(_jwtCacheServiceMock.Object);
    }

    [Test]
    public async Task GenerateJwtToken_WhenCalled_ShouldReturnTokenAndExpiry()
    {
        // Arrange
        var validTo = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _jwtCacheServiceMock
            .Setup(x => x.GenerateAsync(AccountId))
            .ReturnsAsync(new StoredToken { AccountId = AccountId, Token = Token, ValidTo = validTo });

        // Act
        var response = await _service.GenerateJwtToken(
            new GenerateJwtTokenRequest { AccountId = AccountId },
            Mock.Of<ServerCallContext>());

        // Assert
        response.Token.Should().Be(Token);
        response.ExpiresAtUnixSeconds.Should().Be(new DateTimeOffset(validTo).ToUnixTimeSeconds());
    }

    [Test]
    public async Task RevokeJwtToken_WhenCalled_ShouldCallCacheService()
    {
        // Arrange
        var request = new RevokeJwtTokenRequest { Token = Token };

        // Act
        var response = await _service.RevokeJwtToken(request, Mock.Of<ServerCallContext>());

        // Assert
        response.Should().NotBeNull();
        _jwtCacheServiceMock.Verify(x => x.RevokeAsync(Token), Times.Once);
    }
}
