using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Cors.Tests;

[TestFixture]
public class AllowedOriginsTests
{
    private const string AllowedOrigin = "https://letstalk.example.com";
    private const string SecondAllowedOrigin = "https://127.0.0.1:4200";
    private const string OtherOrigin = "https://evil.example";

    [Test]
    public void Contains_WhenOriginConfigured_ShouldBeTrue()
    {
        // Arrange
        var allowedOrigins = Create(AllowedOrigin, SecondAllowedOrigin);

        // Assert
        allowedOrigins.Contains(AllowedOrigin).Should().BeTrue();
        allowedOrigins.Contains(SecondAllowedOrigin).Should().BeTrue();
    }

    [Test]
    public void Contains_WhenOriginUnknown_ShouldBeFalse()
    {
        // Arrange
        var allowedOrigins = Create(AllowedOrigin);

        // Assert
        allowedOrigins.Contains(OtherOrigin).Should().BeFalse();
    }

    [TestCase("https://letstalk.example.com/")]
    [TestCase("HTTPS://LETSTALK.EXAMPLE.COM")]
    public void Contains_WhenRequestOriginDiffersOnlyInSlashOrLetterCase_ShouldBeTrue(string origin)
    {
        // Arrange: host names are case-insensitive and a trailing slash does not change the origin
        var allowedOrigins = Create(AllowedOrigin);

        // Assert
        allowedOrigins.Contains(origin).Should().BeTrue();
    }

    [Test]
    public void Contains_WhenConfiguredOriginHasTrailingSlash_ShouldBeTrue()
    {
        // Arrange
        var allowedOrigins = Create($"{AllowedOrigin}/");

        // Assert
        allowedOrigins.Contains(AllowedOrigin).Should().BeTrue();
    }

    [Test]
    public void Contains_WhenOnlyThePrefixMatches_ShouldBeFalse()
    {
        // Arrange: a longer host name must not pass for a configured one
        var allowedOrigins = Create(AllowedOrigin);

        // Assert
        allowedOrigins.Contains($"{AllowedOrigin}.evil.example").Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("null")]
    public void Contains_WhenOriginMissingOrOpaque_ShouldBeFalse(string? origin)
    {
        // Arrange
        var allowedOrigins = Create(AllowedOrigin);

        // Assert
        allowedOrigins.Contains(origin).Should().BeFalse();
    }

    [Test]
    public void Contains_WhenNothingConfigured_ShouldBeFalse()
    {
        // Arrange
        var allowedOrigins = Create();

        // Assert
        allowedOrigins.Contains(AllowedOrigin).Should().BeFalse();
    }

    [Test]
    public void AddCredentialedCors_ShouldRegisterTheSameOriginsForOtherChecks()
    {
        // Arrange
        var configuration = CreateConfiguration(AllowedOrigin);
        var services = new ServiceCollection();

        // Act
        services.AddCredentialedCors(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert: the origin check of the API and of the notifications hub use the list that CORS uses
        provider.GetRequiredService<AllowedOrigins>().Contains(AllowedOrigin).Should().BeTrue();
    }

    private static AllowedOrigins Create(params string[] origins) => new(CreateConfiguration(origins));

    private static IConfiguration CreateConfiguration(params string[] origins) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(origins.Select((origin, index) =>
                new KeyValuePair<string, string?>($"Cors:AllowedOrigins:{index}", origin)))
            .Build();
}
