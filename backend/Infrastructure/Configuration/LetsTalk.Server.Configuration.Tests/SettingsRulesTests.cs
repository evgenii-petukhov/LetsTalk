using FluentAssertions;
using LetsTalk.Server.Configuration.Validation;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class SettingsRulesTests
{
    private const string Key = "Messaging:MessagesPerPage";

    [TestCase(1)]
    [TestCase(50)]
    [TestCase(100)]
    public void Range_WhenValueIsInside_ShouldReturnNull(int value)
    {
        // Act
        var result = SettingsRules.Range(Key, value, 1, 100);

        // Assert
        result.Should().BeNull();
    }

    [TestCase(0)]
    [TestCase(101)]
    [TestCase(-5)]
    public void Range_WhenValueIsOutside_ShouldNameTheKeyTheRangeAndTheValue(int value)
    {
        // Act
        var result = SettingsRules.Range(Key, value, 1, 100);

        // Assert
        result.Should().Be($"{Key} must be between 1 and 100, but is {value}.");
    }

    [Test]
    public void Range_WhenTimeSpanIsInside_ShouldReturnNull()
    {
        // Act
        var result = SettingsRules.Range(Key, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));

        // Assert
        result.Should().BeNull();
    }

    [TestCase(0)]
    [TestCase(8 * 24 * 60)]
    public void Range_WhenTimeSpanIsOutside_ShouldNameTheKeyAndTheRange(int minutes)
    {
        // Act
        var result = SettingsRules.Range(Key, TimeSpan.FromMinutes(minutes), TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));

        // Assert
        result.Should().StartWith($"{Key} must be between 00:01:00 and 7.00:00:00");
    }

    [Test]
    public void NotEmpty_WhenValueIsSet_ShouldReturnNull()
    {
        // Act
        var result = SettingsRules.NotEmpty("Aws:Region", "eu-west-1");

        // Assert
        result.Should().BeNull();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void NotEmpty_WhenValueIsMissing_ShouldNameTheKey(string? value)
    {
        // Act
        var result = SettingsRules.NotEmpty("Aws:Region", value);

        // Assert
        result.Should().Be("Aws:Region must be set.");
    }
}
