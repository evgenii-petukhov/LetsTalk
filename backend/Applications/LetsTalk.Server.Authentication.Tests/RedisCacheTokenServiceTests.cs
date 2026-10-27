using FluentAssertions;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Exceptions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Authentication.Services.Cache.Token;
using LetsTalk.Server.Persistence.Redis;
using Microsoft.Extensions.Configuration;
using Moq;
using StackExchange.Redis;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class RedisCacheTokenServiceTests
{
    private const string Token = "jwt-token";
    private const string AccountId = "42";
    private const string TokenKey = "jwt:jwt-token";
    private const string RevokedKey = "revoked:jwt:jwt-token";
    private const int LifetimeMinutes = 10;

    private Mock<IJwtStorageService> _jwtStorageServiceMock;
    private Mock<IDatabase> _databaseMock;
    private RedisCacheTokenService _service;

    [SetUp]
    public void SetUp()
    {
        _jwtStorageServiceMock = new Mock<IJwtStorageService>();
        _databaseMock = new Mock<IDatabase>();

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
        var redisConnectionMock = new Mock<RedisConnection>(Mock.Of<IConfiguration>());
        redisConnectionMock
            .SetupGet(x => x.Connection)
            .Returns(multiplexerMock.Object);

        _service = new RedisCacheTokenService(_jwtStorageServiceMock.Object, redisConnectionMock.Object);
    }

    [Test]
    public async Task RevokeAsync_WhenTokenValid_ShouldWriteDenylistKeyWithRemainingLifetime()
    {
        // Arrange
        SetupValidToken();

        // Act
        await _service.RevokeAsync(Token);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k == RevokedKey),
            It.IsAny<RedisValue>(),
            It.Is<Expiration>(e => IsLifetimeCloseToRemaining(e)),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task RevokeAsync_WhenTokenValid_ShouldDeletePositiveCacheKey()
    {
        // Arrange
        SetupValidToken();

        // Act
        await _service.RevokeAsync(Token);

        // Assert
        _databaseMock.Verify(x => x.KeyDeleteAsync(
            It.Is<RedisKey>(k => k == TokenKey),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task RevokeAsync_WhenTokenInvalid_ShouldNotThrowAndNotWriteDenylist()
    {
        // Arrange
        _jwtStorageServiceMock
            .Setup(x => x.GetStoredTokenAsync(Token))
            .ThrowsAsync(new AppException("Invalid token"));

        // Act
        var act = () => _service.RevokeAsync(Token);

        // Assert
        await act.Should().NotThrowAsync();
        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Test]
    public async Task GetAccountIdAsync_WhenTokenRevoked_ShouldReturnNullWithoutReadingCacheOrRevalidating()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.Is<RedisKey>(k => k == RevokedKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.GetAccountIdAsync(Token);

        // Assert
        result.Should().BeNull();
        _databaseMock.Verify(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
        _jwtStorageServiceMock.Verify(x => x.GetStoredTokenAsync(It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task GetAccountIdAsync_WhenTokenNotRevoked_ShouldCheckDenylistBeforeReadingCache()
    {
        // Arrange
        var calls = new List<string>();
        _databaseMock
            .Setup(x => x.KeyExistsAsync(It.Is<RedisKey>(k => k == RevokedKey), It.IsAny<CommandFlags>()))
            .Callback(() => calls.Add("denylist"))
            .ReturnsAsync(false);
        _databaseMock
            .Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == TokenKey), It.IsAny<CommandFlags>()))
            .Callback(() => calls.Add("cache"))
            .ReturnsAsync(new RedisValue("\"" + AccountId + "\""));

        // Act
        var result = await _service.GetAccountIdAsync(Token);

        // Assert
        result.Should().Be(AccountId);
        calls.Should().Equal("denylist", "cache");
    }

    [Test]
    public async Task GenerateAsync_ShouldCacheTheTokenForItsRemainingLifetime()
    {
        // Arrange: ValidTo is UTC, so the lifetime must be measured against UtcNow whatever the server time zone is
        _jwtStorageServiceMock
            .Setup(x => x.Generate(AccountId))
            .Returns(new StoredToken
            {
                AccountId = AccountId,
                Token = Token,
                ValidTo = DateTime.UtcNow.AddMinutes(LifetimeMinutes)
            });

        // Act
        await _service.GenerateAsync(AccountId);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k == TokenKey),
            It.IsAny<RedisValue>(),
            It.Is<Expiration>(e => IsLifetimeCloseToRemaining(e)),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task GetAccountIdAsync_WhenNotCached_ShouldCacheForTheRemainingLifetime()
    {
        // Arrange
        SetupValidToken();

        // Act
        await _service.GetAccountIdAsync(Token);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k == TokenKey),
            It.IsAny<RedisValue>(),
            It.Is<TimeSpan?>(t => t.HasValue
                && t.Value > TimeSpan.FromMinutes(LifetimeMinutes - 1)
                && t.Value <= TimeSpan.FromMinutes(LifetimeMinutes)),
            When.NotExists), Times.Once);
    }

    // Expiration exposes no accessor; its ToString() is the Redis option, "EX <seconds>" or "PX <milliseconds>"
    private static bool IsLifetimeCloseToRemaining(Expiration expiration)
    {
        var parts = expiration.ToString().Split(' ');
        var value = long.Parse(parts[1]);
        var lifetime = parts[0] == "EX" ? TimeSpan.FromSeconds(value) : TimeSpan.FromMilliseconds(value);

        return lifetime > TimeSpan.FromMinutes(LifetimeMinutes - 1)
            && lifetime <= TimeSpan.FromMinutes(LifetimeMinutes);
    }

    private void SetupValidToken()
    {
        _jwtStorageServiceMock
            .Setup(x => x.GetStoredTokenAsync(Token))
            .ReturnsAsync(new StoredToken
            {
                AccountId = AccountId,
                Token = Token,
                ValidTo = DateTime.UtcNow.AddMinutes(LifetimeMinutes)
            });
    }
}
