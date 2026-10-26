using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using Moq;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class JwtTokenGrpcServiceTests
{
    private const string Token = "jwt-token";

    private Mock<IJwtCacheService> _jwtCacheServiceMock;
    private Services.JwtTokenGrpcService _service;

    [SetUp]
    public void SetUp()
    {
        _jwtCacheServiceMock = new Mock<IJwtCacheService>();
        _service = new Services.JwtTokenGrpcService(_jwtCacheServiceMock.Object);
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
