using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class JwtTokenGrpcServiceTests
{
    private const string Token = "jwt-token";
    private const string AccountId = "42";
    private const string RefreshToken = "refresh-token";
    private const string NewRefreshToken = "new-refresh-token";

    private static readonly DateTime AccessExpiresAt = new(2030, 1, 1, 0, 15, 0, DateTimeKind.Utc);
    private static readonly DateTime RefreshExpiresAt = new(2030, 1, 31, 0, 0, 0, DateTimeKind.Utc);

    private Mock<IJwtCacheService> _jwtCacheServiceMock;
    private Mock<IRefreshSessionService> _refreshSessionServiceMock;
    private JwtSettings _jwtSettings;
    private Services.JwtTokenGrpcService _service;

    [SetUp]
    public void SetUp()
    {
        _jwtCacheServiceMock = new Mock<IJwtCacheService>();
        _refreshSessionServiceMock = new Mock<IRefreshSessionService>();
        _jwtSettings = new JwtSettings
        {
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
            BackgroundTokenLifetime = TimeSpan.FromHours(1)
        };
        _service = new Services.JwtTokenGrpcService(
            _jwtCacheServiceMock.Object,
            _refreshSessionServiceMock.Object,
            Options.Create(_jwtSettings));
    }

    [Test]
    public async Task GenerateJwtToken_WhenCalled_ShouldReturnTokenAndExpiry()
    {
        // Arrange
        var validTo = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _jwtCacheServiceMock
            .Setup(x => x.GenerateAsync(AccountId, _jwtSettings.AccessTokenLifetime))
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
    public async Task GenerateJwtToken_WhenKindBackground_ShouldUseBackgroundLifetime()
    {
        // Arrange
        _jwtCacheServiceMock
            .Setup(x => x.GenerateAsync(AccountId, _jwtSettings.BackgroundTokenLifetime))
            .ReturnsAsync(new StoredToken { AccountId = AccountId, Token = Token, ValidTo = AccessExpiresAt });

        // Act
        var response = await _service.GenerateJwtToken(
            new GenerateJwtTokenRequest { AccountId = AccountId, Kind = TokenKind.Background },
            Mock.Of<ServerCallContext>());

        // Assert
        response.Token.Should().Be(Token);
        _jwtCacheServiceMock.Verify(x => x.GenerateAsync(AccountId, _jwtSettings.BackgroundTokenLifetime), Times.Once);
        _jwtCacheServiceMock.Verify(x => x.GenerateAsync(AccountId, _jwtSettings.AccessTokenLifetime), Times.Never);
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

    [Test]
    public async Task CreateSession_WhenCalled_ShouldReturnAccessAndRefreshTokens()
    {
        // Arrange
        _refreshSessionServiceMock
            .Setup(x => x.IssueAsync(AccountId))
            .ReturnsAsync(new SessionTokens
            {
                AccessToken = Token,
                AccessExpiresAt = AccessExpiresAt,
                RefreshToken = RefreshToken,
                RefreshExpiresAt = RefreshExpiresAt
            });

        // Act
        var response = await _service.CreateSession(
            new CreateSessionRequest { AccountId = AccountId },
            Mock.Of<ServerCallContext>());

        // Assert
        response.AccessToken.Should().Be(Token);
        response.AccessExpiresAtUnixSeconds.Should().Be(new DateTimeOffset(AccessExpiresAt).ToUnixTimeSeconds());
        response.RefreshToken.Should().Be(RefreshToken);
        response.RefreshExpiresAtUnixSeconds.Should().Be(new DateTimeOffset(RefreshExpiresAt).ToUnixTimeSeconds());
    }

    [Test]
    public async Task RefreshSession_WhenRotated_ShouldReturnNewTokens()
    {
        // Arrange
        _refreshSessionServiceMock
            .Setup(x => x.RotateAsync(RefreshToken))
            .ReturnsAsync(new SessionTokens
            {
                AccessToken = Token,
                AccessExpiresAt = AccessExpiresAt,
                RefreshToken = NewRefreshToken,
                RefreshExpiresAt = RefreshExpiresAt
            });

        // Act
        var response = await _service.RefreshSession(
            new RefreshSessionRequest { RefreshToken = RefreshToken },
            Mock.Of<ServerCallContext>());

        // Assert
        response.AccessToken.Should().Be(Token);
        response.RefreshToken.Should().Be(NewRefreshToken);
        response.RefreshExpiresAtUnixSeconds.Should().Be(new DateTimeOffset(RefreshExpiresAt).ToUnixTimeSeconds());
    }

    [Test]
    public async Task RefreshSession_WhenReusedWithinGrace_ShouldReturnAccessTokenOnly()
    {
        // Arrange
        _refreshSessionServiceMock
            .Setup(x => x.RotateAsync(RefreshToken))
            .ReturnsAsync(new SessionTokens { AccessToken = Token, AccessExpiresAt = AccessExpiresAt });

        // Act
        var response = await _service.RefreshSession(
            new RefreshSessionRequest { RefreshToken = RefreshToken },
            Mock.Of<ServerCallContext>());

        // Assert
        response.AccessToken.Should().Be(Token);
        response.RefreshToken.Should().BeEmpty();
        response.RefreshExpiresAtUnixSeconds.Should().Be(0);
    }

    [Test]
    public async Task RefreshSession_WhenRefreshTokenRejected_ShouldThrowUnauthenticated()
    {
        // Arrange
        _refreshSessionServiceMock
            .Setup(x => x.RotateAsync(RefreshToken))
            .ReturnsAsync((SessionTokens?)null);

        // Act
        var act = () => _service.RefreshSession(
            new RefreshSessionRequest { RefreshToken = RefreshToken },
            Mock.Of<ServerCallContext>());

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [Test]
    public async Task EndSession_WhenCalled_ShouldEndTheChain()
    {
        // Act
        var response = await _service.EndSession(
            new EndSessionRequest { RefreshToken = RefreshToken },
            Mock.Of<ServerCallContext>());

        // Assert
        response.Should().NotBeNull();
        _refreshSessionServiceMock.Verify(x => x.EndByRefreshTokenAsync(RefreshToken), Times.Once);
    }
}
