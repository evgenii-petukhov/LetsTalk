using FluentAssertions;
using LetsTalk.Server.Configuration.Validation;
using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class HttpRetrySettingsTests
{
    private readonly HttpRetrySettingsValidator _validator = new();

    [Test]
    public void New_ShouldCarryTheShippedValues()
    {
        // Act
        var settings = new HttpRetrySettings();

        // Assert
        settings.MaxRetryAttempts.Should().Be(3);
        settings.Delay.Should().Be(TimeSpan.FromSeconds(1));
        settings.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));
        settings.AttemptTimeout.Should().Be(TimeSpan.FromSeconds(30));
        settings.TotalTimeout.Should().Be(TimeSpan.FromSeconds(100));
    }

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(10)]
    public void Validate_WhenMaxRetryAttemptsIsInsideTheRange_ShouldSucceed(int value)
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings { MaxRetryAttempts = value });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void Validate_WhenMaxRetryAttemptsIsOutsideTheRange_ShouldFailAndNameTheKey(int value)
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings { MaxRetryAttempts = value });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Http:MaxRetryAttempts must be between 0 and 10");
    }

    [TestCase("00:00:00")]
    [TestCase("-00:00:01")]
    [TestCase("00:00:00.099")]
    [TestCase("00:01:01")]
    public void Validate_WhenDelayIsOutsideTheRange_ShouldFailAndNameTheKey(string delay)
    {
        // Act: MaxDelay stays at its limit so only the Delay rule can fail
        var result = _validator.Validate(null, new HttpRetrySettings { Delay = TimeSpan.Parse(delay), MaxDelay = TimeSpan.FromMinutes(5) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Http:Delay");
    }

    [Test]
    public void Validate_WhenMaxDelayIsShorterThanDelay_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings { Delay = TimeSpan.FromSeconds(5), MaxDelay = TimeSpan.FromSeconds(4) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Http:MaxDelay");
    }

    [Test]
    public void Validate_WhenMaxDelayEqualsDelay_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings { Delay = TimeSpan.FromSeconds(5), MaxDelay = TimeSpan.FromSeconds(5) });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenMaxDelayIsAboveTheMaximum_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings { MaxDelay = TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(1)) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Http:MaxDelay");
    }

    [TestCase("00:00:00")]
    [TestCase("00:00:00.999")]
    [TestCase("00:10:01")]
    public void Validate_WhenAttemptTimeoutIsOutsideTheRange_ShouldFailAndNameTheKey(string timeout)
    {
        // Act: the total limit stays at its maximum so only the attempt rule can fail
        var result = _validator.Validate(null, new HttpRetrySettings { AttemptTimeout = TimeSpan.Parse(timeout), TotalTimeout = TimeSpan.FromMinutes(30) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Http:AttemptTimeout");
    }

    [Test]
    public void Validate_WhenTotalTimeoutIsShorterThanAttemptTimeout_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings { AttemptTimeout = TimeSpan.FromSeconds(30), TotalTimeout = TimeSpan.FromSeconds(29) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Http:TotalTimeout");
    }

    [Test]
    public void Validate_WhenSeveralRulesBreak_ShouldListEveryOne()
    {
        // Act
        var result = _validator.Validate(null, new HttpRetrySettings { MaxRetryAttempts = -1, Delay = TimeSpan.Zero });

        // Assert
        result.Failures.Should().HaveCount(2);
    }
}
