using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class CachingSettingsValidatorTests
{
    private readonly CachingSettingsValidator _validator = new();

    private static readonly string[] CacheLifeTimes =
    [
        nameof(CachingSettings.MessagesCacheLifeTimeInSeconds),
        nameof(CachingSettings.ChatCacheLifeTimeInSeconds),
        nameof(CachingSettings.AccountCacheLifeTimeInSeconds),
        nameof(CachingSettings.ProfileCacheLifeTimeInSeconds),
        nameof(CachingSettings.ImagesCacheLifeTimeInSeconds)
    ];

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new CachingSettings());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    // -1 = never expires, 0 = cache off (FR-008)
    [Test]
    public void Validate_WhenCacheLifeTimeIsInsideTheRange_ShouldSucceed(
        [ValueSource(nameof(CacheLifeTimes))] string property,
        [Values(-1, 0, 1, 604800)] int value)
    {
        // Arrange
        var settings = With(property, value);

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenCacheLifeTimeIsOutsideTheRange_ShouldFailAndNameTheKey(
        [ValueSource(nameof(CacheLifeTimes))] string property,
        [Values(-2, 604801)] int value)
    {
        // Arrange
        var settings = With(property, value);

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain($"Caching:{property} must be between -1 and 604800");
    }

    [TestCase(1)]
    [TestCase(3600)]
    public void Validate_WhenLoginCodeLifeTimeIsInsideTheRange_ShouldSucceed(int value)
    {
        // Act
        var result = _validator.Validate(null, new CachingSettings { LoginCodeCacheLifeTimeInSeconds = value });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(3601)]
    public void Validate_WhenLoginCodeLifeTimeIsOutsideTheRange_ShouldFailAndNameTheKey(int value)
    {
        // Act
        var result = _validator.Validate(null, new CachingSettings { LoginCodeCacheLifeTimeInSeconds = value });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Caching:LoginCodeCacheLifeTimeInSeconds must be between 1 and 3600");
    }

    [TestCase(0)]
    [TestCase(102400)]
    [TestCase(10485760)]
    public void Validate_WhenThresholdIsInsideTheRange_ShouldSucceed(int value)
    {
        // Act
        var result = _validator.Validate(null, new CachingSettings { ImageSizeThresholdInBytes = value });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(-1)]
    [TestCase(10485761)]
    public void Validate_WhenThresholdIsOutsideTheRange_ShouldFailAndNameTheKey(int value)
    {
        // Act
        var result = _validator.Validate(null, new CachingSettings { ImageSizeThresholdInBytes = value });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Caching:ImageSizeThresholdInBytes must be between 0 and 10485760");
    }

    [Test]
    public void Validate_WhenSeveralValuesAreWrong_ShouldListThemAll()
    {
        // Act
        var result = _validator.Validate(null, new CachingSettings
        {
            ChatCacheLifeTimeInSeconds = -7,
            LoginCodeCacheLifeTimeInSeconds = 0,
            ImageSizeThresholdInBytes = -1
        });

        // Assert
        result.Failures.Should().HaveCount(3);
    }

    private static CachingSettings With(string property, int value)
    {
        var settings = new CachingSettings();
        typeof(CachingSettings).GetProperty(property)!.SetValue(settings, value);
        return settings;
    }
}
