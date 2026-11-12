using FluentAssertions;
using LetsTalk.Server.KeepAlive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;
using StackExchange.Redis;

namespace LetsTalk.Server.Persistence.Redis.Tests;

[TestFixture]
public class RedisKeepAliveTargetTests
{
    private const string KeepAliveKey = "keepalive:letstalk";
    private const string ExpectedExpiry = "EX 3600";
    private const string FailureMessage = "the connection was lost";

    private Mock<IDatabase> _databaseMock;
    private FakeTimeProvider _time;
    private RedisKeepAliveTarget _target;
    private RedisValue _stored;

    [SetUp]
    public void SetUp()
    {
        _stored = RedisValue.Null;
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 12, 8, 30, 0, TimeSpan.Zero));
        _databaseMock = new Mock<IDatabase>();
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, value, _, _, _) => _stored = value)
            .ReturnsAsync(true);
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => _stored);

        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
        var connectionMock = new Mock<RedisConnection>(Mock.Of<IConfiguration>());
        connectionMock.SetupGet(x => x.Connection).Returns(multiplexerMock.Object);

        _target = new RedisKeepAliveTarget(connectionMock.Object, _time);
    }

    [Test]
    public void Name_ShouldBeRedis()
    {
        _target.Name.Should().Be("Redis");
    }

    [Test]
    public async Task KeepAliveAsync_ShouldSetTheKeepAliveKeyWithAnExpiryAndReadItBack()
    {
        // Act
        await _target.KeepAliveAsync(CancellationToken.None);

        // Assert: the value is the current UTC time, the key expires after an hour, and the same key is read back
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(key => key == KeepAliveKey),
            It.Is<RedisValue>(value => value == _time.GetUtcNow().ToString("O")),
            It.Is<Expiration>(expiry => expiry.ToString() == ExpectedExpiry),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Once);
        _databaseMock.Verify(x => x.StringGetAsync(
            It.Is<RedisKey>(key => key == KeepAliveKey),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task KeepAliveAsync_ShouldNotTouchAnyOtherKey()
    {
        // Act
        await _target.KeepAliveAsync(CancellationToken.None);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(key => key != KeepAliveKey),
            It.IsAny<RedisValue>(),
            It.IsAny<Expiration>(),
            It.IsAny<ValueCondition>(),
            It.IsAny<CommandFlags>()), Times.Never);
        _databaseMock.Verify(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Test]
    public async Task KeepAliveAsync_WhenTheValueIsMissingOnReadBack_ShouldThrow()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var act = () => _target.KeepAliveAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task KeepAliveAsync_WhenTheValueDiffersOnReadBack_ShouldThrow()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("another value");

        // Act
        var act = () => _target.KeepAliveAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task KeepAliveAsync_WhenRedisThrows_ShouldPropagate()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new TimeoutException(FailureMessage));

        // Act
        var act = () => _target.KeepAliveAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>().WithMessage(FailureMessage);
    }

    [Test]
    public async Task KeepAliveAsync_WhenCancelledWhileWaiting_ShouldThrowOperationCanceled()
    {
        // Arrange: the Redis client has no cancellation, so the call must stop waiting on its own
        using var cancellation = new CancellationTokenSource();
        _databaseMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<Expiration>(),
                It.IsAny<ValueCondition>(),
                It.IsAny<CommandFlags>()))
            .Returns(new TaskCompletionSource<bool>().Task);

        // Act
        var run = _target.KeepAliveAsync(cancellation.Token);
        await cancellation.CancelAsync();

        // Assert
        await run.Invoking(task => task).Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void AddRedisCache_ShouldRegisterTheRedisKeepAliveTarget()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRedisCache();

        // Assert
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IKeepAliveTarget)
            && descriptor.ImplementationType == typeof(RedisKeepAliveTarget));
    }
}
