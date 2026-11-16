using FluentAssertions;
using LetsTalk.Server.Configuration.Validation;
using LetsTalk.Server.Resilience;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class GrpcRetrySettingsTests
{
    private readonly GrpcRetrySettingsValidator _validator = new();

    [Test]
    public void New_ShouldCarryTheShippedValues()
    {
        // Act
        var settings = new GrpcRetrySettings();

        // Assert
        settings.MaxAttempts.Should().Be(4);
        settings.InitialBackoff.Should().Be(TimeSpan.FromSeconds(1));
        settings.MaxBackoff.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new GrpcRetrySettings());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(1)]
    [TestCase(5)]
    public void Validate_WhenMaxAttemptsIsInsideTheRange_ShouldSucceed(int value)
    {
        // Act
        var result = _validator.Validate(null, new GrpcRetrySettings { MaxAttempts = value });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    // gRPC allows at most five attempts
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(6)]
    public void Validate_WhenMaxAttemptsIsOutsideTheRange_ShouldFailAndNameTheKey(int value)
    {
        // Act
        var result = _validator.Validate(null, new GrpcRetrySettings { MaxAttempts = value });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Grpc:MaxAttempts must be between 1 and 5");
    }

    [TestCase("00:00:00")]
    [TestCase("-00:00:01")]
    [TestCase("00:01:01")]
    public void Validate_WhenInitialBackoffIsOutsideTheRange_ShouldFailAndNameTheKey(string backoff)
    {
        // Act
        var result = _validator.Validate(null, new GrpcRetrySettings { InitialBackoff = TimeSpan.Parse(backoff), MaxBackoff = TimeSpan.FromMinutes(5) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Grpc:InitialBackoff");
    }

    [Test]
    public void Validate_WhenMaxBackoffIsShorterThanInitialBackoff_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new GrpcRetrySettings { InitialBackoff = TimeSpan.FromSeconds(5), MaxBackoff = TimeSpan.FromSeconds(4) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Grpc:MaxBackoff");
    }

    [Test]
    public void Validate_WhenMaxBackoffIsAboveTheMaximum_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new GrpcRetrySettings { MaxBackoff = TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(1)) });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Resilience:Grpc:MaxBackoff");
    }
}
