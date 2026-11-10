using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class JwtSettingsTests
{
    private const string SectionName = "JwtSettings";
    private const string CustomKey = "a-test-key";

    [Test]
    public void JwtSettings_WhenSectionMissing_ShouldUseDefaultLifetimes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var settings = new JwtSettings();

        // Act
        configuration.GetSection(SectionName).Bind(settings);

        // Assert
        settings.Key.Should().BeNull();
        settings.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(15));
        settings.BackgroundTokenLifetime.Should().Be(TimeSpan.FromHours(1));
        settings.RefreshIdleLifetime.Should().Be(TimeSpan.FromDays(30));
        settings.RefreshAbsoluteLifetime.Should().Be(TimeSpan.FromDays(90));
        settings.RefreshReuseGrace.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void JwtSettings_WhenSectionPresent_ShouldBindValues()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SectionName}:Key"] = CustomKey,
                [$"{SectionName}:AccessTokenLifetime"] = "00:01:00",
                [$"{SectionName}:BackgroundTokenLifetime"] = "02:00:00",
                [$"{SectionName}:RefreshIdleLifetime"] = "1.00:00:00",
                [$"{SectionName}:RefreshAbsoluteLifetime"] = "7.00:00:00",
                [$"{SectionName}:RefreshReuseGrace"] = "00:00:10"
            })
            .Build();
        var settings = new JwtSettings();

        // Act
        configuration.GetSection(SectionName).Bind(settings);

        // Assert
        settings.Key.Should().Be(CustomKey);
        settings.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(1));
        settings.BackgroundTokenLifetime.Should().Be(TimeSpan.FromHours(2));
        settings.RefreshIdleLifetime.Should().Be(TimeSpan.FromDays(1));
        settings.RefreshAbsoluteLifetime.Should().Be(TimeSpan.FromDays(7));
        settings.RefreshReuseGrace.Should().Be(TimeSpan.FromSeconds(10));
    }
}
