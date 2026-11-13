using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class SecuritySettingsValidatorTests
{
    private readonly SecuritySettingsValidator _validator = new();

    [TestCase(1)]
    [TestCase(60)]
    [TestCase(3600)]
    public void Validate_WhenLifeTimeIsInsideTheRange_ShouldSucceed(int value)
    {
        // Act
        var result = _validator.Validate(null, new SecuritySettings { AntiSpamTokenLifeTimeInSeconds = value });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-5)]
    [TestCase(3601)]
    public void Validate_WhenLifeTimeIsOutsideTheRange_ShouldFailAndNameTheKey(int value)
    {
        // Act
        var result = _validator.Validate(null, new SecuritySettings { AntiSpamTokenLifeTimeInSeconds = value });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Security:AntiSpamTokenLifeTimeInSeconds must be between 1 and 3600");
    }

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new SecuritySettings());

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
