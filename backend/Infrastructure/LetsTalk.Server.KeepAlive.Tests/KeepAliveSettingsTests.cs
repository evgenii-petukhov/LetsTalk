using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.KeepAlive.Tests;

[TestFixture]
public class KeepAliveSettingsTests
{
    private const string SectionName = "KeepAlive";
    private const string RedisName = "Redis";
    private const string MongoName = "MongoDB";

    [Test]
    public void New_ShouldHaveShortStartupDelayAndNoTargets()
    {
        // Act
        var settings = new KeepAliveSettings();

        // Assert
        settings.StartupDelay.Should().Be(TimeSpan.FromSeconds(30));
        settings.Targets.Should().BeEmpty();
    }

    [Test]
    public void TargetSettings_New_ShouldBeEnabledWithDailyInterval()
    {
        // Act
        var settings = new KeepAliveTargetSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.Interval.Should().Be(TimeSpan.FromDays(1));
    }

    [Test]
    public void Bind_WhenTargetsGiven_ShouldFillEachTargetSeparately()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SectionName}:StartupDelay"] = "00:00:05",
                [$"{SectionName}:Targets:{RedisName}:Interval"] = "12:00:00",
                [$"{SectionName}:Targets:{MongoName}:Enabled"] = "false",
            })
            .Build();

        // Act
        var settings = configuration.GetSection(SectionName).Get<KeepAliveSettings>()!;

        // Assert
        settings.StartupDelay.Should().Be(TimeSpan.FromSeconds(5));
        settings.Targets[RedisName].Enabled.Should().BeTrue();
        settings.Targets[RedisName].Interval.Should().Be(TimeSpan.FromHours(12));
        settings.Targets[MongoName].Enabled.Should().BeFalse();
        settings.Targets[MongoName].Interval.Should().Be(TimeSpan.FromDays(1));
    }

    [Test]
    public void Bind_WhenTargetNameHasAnotherLetterCase_ShouldStillMatch()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SectionName}:Targets:mongodb:Interval"] = "06:00:00",
            })
            .Build();

        // Act
        var settings = configuration.GetSection(SectionName).Get<KeepAliveSettings>()!;

        // Assert
        settings.Targets[MongoName].Interval.Should().Be(TimeSpan.FromHours(6));
    }

    [Test]
    public void Bind_WhenSectionMissing_ShouldKeepTheDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var settings = configuration.GetSection(SectionName).Get<KeepAliveSettings>() ?? new KeepAliveSettings();

        // Assert
        settings.StartupDelay.Should().Be(TimeSpan.FromSeconds(30));
        settings.Targets.Should().BeEmpty();
    }
}
