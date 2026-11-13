using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class CloudflareSettingsValidatorTests
{
    private const string Secret = "secret-value";

    private readonly CloudflareSettingsValidator _validator = new();

    [TestCase(60)]
    [TestCase(86400)]
    [TestCase(172800)]
    public void Validate_WhenTokenTtlIsInsideTheRange_ShouldSucceed(int value)
    {
        // Act
        var result = _validator.Validate(null, new CloudflareSettings { TurnTokenId = Secret, ApiToken = Secret, TokenTtl = value });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(59)]
    [TestCase(172801)]
    public void Validate_WhenTokenTtlIsOutsideTheRange_ShouldFailAndNameTheKey(int value)
    {
        // Act
        var result = _validator.Validate(null, new CloudflareSettings { TurnTokenId = Secret, ApiToken = Secret, TokenTtl = value });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Cloudflare:TokenTtl must be between 60 and 172800");
    }

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act: the credentials have no default, so they are set here
        var result = _validator.Validate(null, new CloudflareSettings { TurnTokenId = Secret, ApiToken = Secret });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_WhenACredentialIsEmpty_ShouldFailAndNameOnlyThatKey(string? value)
    {
        // Act
        var result = _validator.Validate(null, new CloudflareSettings { TurnTokenId = value, ApiToken = Secret });

        // Assert
        result.Failures.Should().ContainSingle().Which.Should().Be("Cloudflare:TurnTokenId must be set.");
        _validator.Validate(null, new CloudflareSettings { TurnTokenId = Secret, ApiToken = value })
            .Failures.Should().ContainSingle().Which.Should().Be("Cloudflare:ApiToken must be set.");
    }

    [Test]
    public void Validate_WhenBothCredentialsAndTheTtlAreWrong_ShouldListAllThree()
    {
        // Act
        var result = _validator.Validate(null, new CloudflareSettings { TokenTtl = 0 });

        // Assert
        result.Failures.Should().HaveCount(3);
    }
}
