using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.KeepAlive.Tests;

[TestFixture]
public class KeepAliveSettingsTests
{
    private const string SectionName = "KeepAlive";

    [Test]
    public void New_ShouldBeEnabledWithDailyIntervalAndShortStartupDelay()
    {
        // Act
        var settings = new KeepAliveSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.Interval.Should().Be(TimeSpan.FromDays(1));
        settings.StartupDelay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void Bind_WhenValuesGiven_ShouldOverrideTheDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SectionName}:Enabled"] = "false",
                [$"{SectionName}:Interval"] = "06:00:00",
                [$"{SectionName}:StartupDelay"] = "00:00:05",
            })
            .Build();

        // Act
        var settings = configuration.GetSection(SectionName).Get<KeepAliveSettings>()!;

        // Assert
        settings.Enabled.Should().BeFalse();
        settings.Interval.Should().Be(TimeSpan.FromHours(6));
        settings.StartupDelay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Test]
    public void Bind_WhenSectionMissing_ShouldKeepTheDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var settings = configuration.GetSection(SectionName).Get<KeepAliveSettings>() ?? new KeepAliveSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.Interval.Should().Be(TimeSpan.FromDays(1));
    }
}
