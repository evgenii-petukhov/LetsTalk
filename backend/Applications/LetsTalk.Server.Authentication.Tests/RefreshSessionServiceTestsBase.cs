using FluentAssertions;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.Authentication.Tests;

// The rotation rules live in one base class, so the memory and the Redis stores are tested with the same scenarios.
public abstract class RefreshSessionServiceTestsBase
{
    protected const string AccountId = "42";

    private static readonly DateTime Start = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private int _accessTokenCounter;

    protected JwtSettings Settings { get; private set; } = null!;

    protected TestTimeProvider Time { get; private set; } = null!;

    protected IRefreshSessionService Service { get; private set; } = null!;

    protected Mock<IJwtCacheService> JwtCacheServiceMock { get; private set; } = null!;

    protected ListLogger Logger { get; private set; } = null!;

    [SetUp]
    public void SetUpBase()
    {
        Settings = new JwtSettings();
        Time = new TestTimeProvider(new DateTimeOffset(Start));
        Logger = new ListLogger();
        _accessTokenCounter = 0;
        JwtCacheServiceMock = new Mock<IJwtCacheService>();
        JwtCacheServiceMock
            .Setup(x => x.GenerateAsync(AccountId, It.IsAny<TimeSpan>()))
            .ReturnsAsync((string accountId, TimeSpan lifetime) => new StoredToken
            {
                AccountId = accountId,
                Token = $"access-{Interlocked.Increment(ref _accessTokenCounter)}",
                ValidTo = Time.GetUtcNow().UtcDateTime + lifetime
            });

        Service = CreateService(JwtCacheServiceMock.Object, Options.Create(Settings), Time, Logger);
    }

    protected abstract IRefreshSessionService CreateService(
        IJwtCacheService jwtCacheService,
        IOptions<JwtSettings> jwtSettings,
        TimeProvider timeProvider,
        ListLogger logger);

    // Every key and value the store holds, as text, to prove that a raw refresh token is never written
    protected abstract IEnumerable<string> GetStoredText();

    [Test]
    public async Task IssueAsync_ShouldReturnAccessAndRefreshTokensWithTheirExpiry()
    {
        // Act
        var tokens = await Service.IssueAsync(AccountId);

        // Assert
        tokens.AccessToken.Should().Be("access-1");
        tokens.AccessExpiresAt.Should().Be(Start + Settings.AccessTokenLifetime);
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshExpiresAt.Should().Be(Start + Settings.RefreshIdleLifetime);
        JwtCacheServiceMock.Verify(x => x.GenerateAsync(AccountId, Settings.AccessTokenLifetime), Times.Once);
    }

    [Test]
    public async Task IssueAsync_ShouldStoreOnlyHash()
    {
        // Act
        var tokens = await Service.IssueAsync(AccountId);

        // Assert
        var stored = GetStoredText().ToList();
        stored.Should().NotBeEmpty();
        stored.Should().OnlyContain(x => !x.Contains(tokens.RefreshToken!));
    }

    [Test]
    public async Task IssueAsync_WhenCalledTwice_ShouldIssueDifferentRefreshTokens()
    {
        // Act
        var first = await Service.IssueAsync(AccountId);
        var second = await Service.IssueAsync(AccountId);

        // Assert
        first.RefreshToken.Should().NotBe(second.RefreshToken);
    }

    [Test]
    public async Task RotateAsync_WhenUnused_ShouldIssueNewPair()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);
        Time.Advance(TimeSpan.FromMinutes(20));

        // Act
        var rotated = await Service.RotateAsync(issued.RefreshToken!);

        // Assert
        rotated.Should().NotBeNull();
        rotated!.AccessToken.Should().Be("access-2");
        rotated.RefreshToken.Should().NotBeNullOrWhiteSpace().And.NotBe(issued.RefreshToken);
    }

    [Test]
    public async Task RotateAsync_WhenUnused_ShouldSlideTheIdlePeriod()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);
        Time.Advance(TimeSpan.FromDays(10));

        // Act
        var rotated = await Service.RotateAsync(issued.RefreshToken!);

        // Assert
        rotated!.RefreshExpiresAt.Should().Be(Start + TimeSpan.FromDays(10) + Settings.RefreshIdleLifetime);
    }

    [Test]
    public async Task RotateAsync_WhenUsedWithinGrace_ShouldIssueAccessOnly()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);
        await Service.RotateAsync(issued.RefreshToken!);
        Time.Advance(Settings.RefreshReuseGrace - TimeSpan.FromSeconds(1));

        // Act
        var second = await Service.RotateAsync(issued.RefreshToken!);

        // Assert
        second.Should().NotBeNull();
        second!.AccessToken.Should().Be("access-3");
        second.RefreshToken.Should().BeNull();
        second.RefreshExpiresAt.Should().BeNull();
    }

    [Test]
    public async Task RotateAsync_WhenUsedAfterGrace_ShouldEndChainAndFail()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);
        var successor = await Service.RotateAsync(issued.RefreshToken!);
        Time.Advance(Settings.RefreshReuseGrace + TimeSpan.FromSeconds(1));

        // Act
        var replayed = await Service.RotateAsync(issued.RefreshToken!);
        var successorAfterwards = await Service.RotateAsync(successor!.RefreshToken!);

        // Assert
        replayed.Should().BeNull();
        successorAfterwards.Should().BeNull();
    }

    [Test]
    public async Task RotateAsync_WhenUsedAfterGrace_ShouldLogWarningWithoutTokenValues()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);
        var successor = await Service.RotateAsync(issued.RefreshToken!);
        Time.Advance(Settings.RefreshReuseGrace + TimeSpan.FromSeconds(1));

        // Act
        await Service.RotateAsync(issued.RefreshToken!);

        // Assert
        var warnings = Logger.Entries.Where(x => x.Level == LogLevel.Warning).ToList();
        warnings.Should().ContainSingle();
        warnings[0].Message.Should().NotContain(issued.RefreshToken!).And.NotContain(successor!.RefreshToken!);
    }

    [Test]
    public async Task RotateAsync_WhenIdleExpired_ShouldFail()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);
        Time.Advance(Settings.RefreshIdleLifetime + TimeSpan.FromSeconds(1));

        // Act
        var rotated = await Service.RotateAsync(issued.RefreshToken!);

        // Assert
        rotated.Should().BeNull();
    }

    [Test]
    public async Task RotateAsync_WhenAbsoluteLifetimeReached_ShouldFail()
    {
        // Arrange: an active user renews every 20 days; the session still ends 90 days after the sign-in
        var tokens = await Service.IssueAsync(AccountId);
        for (var i = 0; i < 4; i++)
        {
            Time.Advance(TimeSpan.FromDays(20));
            tokens = (await Service.RotateAsync(tokens.RefreshToken!))!;
        }

        tokens.RefreshExpiresAt.Should().Be(Start + Settings.RefreshAbsoluteLifetime);
        Time.Advance(TimeSpan.FromDays(11));

        // Act
        var rotated = await Service.RotateAsync(tokens.RefreshToken!);

        // Assert
        rotated.Should().BeNull();
    }

    [Test]
    public async Task RotateAsync_WhenUnknownToken_ShouldFail()
    {
        // Act
        var rotated = await Service.RotateAsync("unknown-token");

        // Assert
        rotated.Should().BeNull();
    }

    [Test]
    public async Task RotateAsync_WhenConcurrent_ShouldCreateSingleSuccessor()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);

        // Act
        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => Task.Run(() => Service.RotateAsync(issued.RefreshToken!))));

        // Assert
        results.Should().OnlyContain(x => x != null);
        results.Count(x => x!.RefreshToken != null).Should().Be(1);
    }

    [Test]
    public async Task EndByRefreshTokenAsync_ShouldInvalidateChain()
    {
        // Arrange
        var issued = await Service.IssueAsync(AccountId);
        var successor = await Service.RotateAsync(issued.RefreshToken!);

        // Act
        await Service.EndByRefreshTokenAsync(successor!.RefreshToken!);

        // Assert: neither the current token nor the one still inside its grace period works
        (await Service.RotateAsync(successor.RefreshToken!)).Should().BeNull();
        (await Service.RotateAsync(issued.RefreshToken!)).Should().BeNull();
    }

    [Test]
    public async Task EndByRefreshTokenAsync_ShouldNotAffectAnotherChain()
    {
        // Arrange
        var first = await Service.IssueAsync(AccountId);
        var second = await Service.IssueAsync(AccountId);

        // Act
        await Service.EndByRefreshTokenAsync(first.RefreshToken!);

        // Assert
        (await Service.RotateAsync(second.RefreshToken!)).Should().NotBeNull();
    }

    [Test]
    public async Task EndByRefreshTokenAsync_WhenUnknownToken_ShouldNotThrow()
    {
        // Act
        var act = () => Service.EndByRefreshTokenAsync("unknown-token");

        // Assert
        await act.Should().NotThrowAsync();
    }
}
