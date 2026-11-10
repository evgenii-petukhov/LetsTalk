using FluentAssertions;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Services.Session;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Persistence.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class RedisRefreshSessionServiceTests : RefreshSessionServiceTestsBase
{
    private readonly object _lock = new();
    private readonly Dictionary<string, string> _store = [];

    private Mock<IDatabase> _databaseMock = null!;

    protected override IRefreshSessionService CreateService(
        IJwtCacheService jwtCacheService,
        IOptions<JwtSettings> jwtSettings,
        TimeProvider timeProvider,
        ListLogger logger)
    {
        _store.Clear();
        _databaseMock = CreateDatabase();

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
        var redisConnectionMock = new Mock<RedisConnection>(Mock.Of<IConfiguration>());
        redisConnectionMock
            .SetupGet(x => x.Connection)
            .Returns(multiplexerMock.Object);

        return new RedisRefreshSessionService(
            jwtCacheService,
            jwtSettings,
            timeProvider,
            new TypedLogger(logger),
            redisConnectionMock.Object);
    }

    protected override IEnumerable<string> GetStoredText()
    {
        lock (_lock)
        {
            return _store.SelectMany(x => new[] { x.Key, x.Value }).ToList();
        }
    }

    [Test]
    public async Task RotateAsync_ShouldMarkTheTokenAsUsedOnlyIfNotYetMarked()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);

        // Act
        await Service.RotateAsync(issued.RefreshToken!);

        // Assert: the marker is written with When.NotExists, so two simultaneous rotations cannot both win
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().StartsWith("refresh-used:")),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            When.NotExists), Times.Once);
    }

    [Test]
    public async Task IssueAsync_ShouldGiveEveryKeyAnExpiry()
    {
        // Act
        await Service.IssueAsync(AccountId);

        // Assert: nothing is stored without a lifetime, otherwise sessions would never leave Redis
        // (Expiration exposes no accessor; its ToString() is the Redis option, "EX <seconds>" or "PX <milliseconds>")
        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.Is<Expiration>(e => e.ToString().StartsWith("EX ") || e.ToString().StartsWith("PX ")),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Exactly(2));
        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.Is<Expiration>(e => !e.ToString().StartsWith("EX ") && !e.ToString().StartsWith("PX ")),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    // A dictionary behind the few commands the service uses; SET NX is atomic, as in Redis
    private Mock<IDatabase> CreateDatabase()
    {
        var database = new Mock<IDatabase>();

        database
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                lock (_lock)
                {
                    return _store.TryGetValue(key.ToString(), out var value) ? (RedisValue)value : RedisValue.Null;
                }
            });
        database
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(), It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue value, Expiration _, ValueCondition _, CommandFlags _) =>
            {
                lock (_lock)
                {
                    _store[key.ToString()] = value.ToString();
                    return true;
                }
            });
        database
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>()))
            .ReturnsAsync((RedisKey key, RedisValue value, TimeSpan? _, When when) =>
            {
                lock (_lock)
                {
                    if (when == When.NotExists && _store.ContainsKey(key.ToString()))
                    {
                        return false;
                    }

                    _store[key.ToString()] = value.ToString();
                    return true;
                }
            });
        database
            .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                lock (_lock)
                {
                    return _store.Remove(key.ToString());
                }
            });

        return database;
    }

    private sealed class TypedLogger(ILogger inner) : ILogger<RedisRefreshSessionService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
