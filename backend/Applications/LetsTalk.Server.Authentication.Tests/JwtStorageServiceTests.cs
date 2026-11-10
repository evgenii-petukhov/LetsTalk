using FluentAssertions;
using LetsTalk.Server.Authentication.Services.Cache.Token;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class JwtStorageServiceTests
{
    private const string AccountId = "42";
    private const string Key = "a-test-signing-key-of-sufficient-length";

    private JwtStorageService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new JwtStorageService(Options.Create(new JwtSettings { Key = Key }));
    }

    [TestCase(15)]
    [TestCase(60)]
    public void Generate_WhenLifetimeGiven_ShouldSetExpiry(int minutes)
    {
        // Arrange
        var lifetime = TimeSpan.FromMinutes(minutes);
        var before = DateTime.UtcNow;

        // Act
        var storedToken = _service.Generate(AccountId, lifetime);

        // Assert: the JWT expiry has a resolution of one second
        storedToken.AccountId.Should().Be(AccountId);
        storedToken.ValidTo.Should().BeOnOrAfter(before + lifetime - TimeSpan.FromSeconds(1));
        storedToken.ValidTo.Should().BeOnOrBefore(DateTime.UtcNow + lifetime + TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task Generate_WhenLifetimeGiven_ShouldProduceATokenThatValidatesWithTheSameExpiry()
    {
        // Arrange
        var generated = _service.Generate(AccountId, TimeSpan.FromMinutes(15));

        // Act
        var stored = await _service.GetStoredTokenAsync(generated.Token);

        // Assert
        stored!.AccountId.Should().Be(AccountId);
        stored.ValidTo.Should().Be(generated.ValidTo);
    }
}
