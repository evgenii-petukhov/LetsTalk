using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class JwtSettingsValidatorTests
{
    private const string ValidKey = "0123456789abcdef0123456789abcdef"; // 32 bytes

    private readonly JwtSettingsValidator _validator = new();

    [Test]
    public void Validate_WhenKeyAndLifetimesAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings { Key = ValidKey });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("0123456789abcdef0123456789abcde")] // 31 bytes
    public void Validate_WhenKeyIsMissingOrShort_ShouldFailWithoutShowingTheKey(string? key)
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings { Key = key });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("JwtSettings:Key must be at least 32 bytes");
        if (!string.IsNullOrWhiteSpace(key))
        {
            result.FailureMessage.Should().NotContain(key);
        }
    }

    [TestCase("0123456789abcdef0123456789abcdef")] // 32 ASCII characters
    [TestCase("0123456789abcdef0123456789abcdef0123")] // 36 ASCII characters
    [TestCase("q+/Zk3v9L0m2Xr8T5yB1nC4wE7hJ6uA=")] // base64 alphabet
    public void Validate_WhenKeyHasAtLeast32AsciiCharacters_ShouldSucceed(string key)
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings { Key = key });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    // The signing code reads the key as ASCII, so any other character would silently become '?'
    [TestCase("ёёёёёёёёёёёёёёёё")] // 16 two-byte characters (32 UTF-8 bytes)
    [TestCase("ёёёёёёёёёёёёёёёёё")] // 17 two-byte characters
    [TestCase("0123456789abcdef0123456789abcdeé")] // 32 characters, one is not ASCII
    public void Validate_WhenKeyHasANonAsciiCharacter_ShouldFailWithoutShowingTheKey(string key)
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings { Key = key });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("JwtSettings:Key").And.Contain("ASCII").And.NotContain(key);
    }

    [TestCase("00:01:00")]
    [TestCase("1.00:00:00")] // the Development value
    [TestCase("7.00:00:00")]
    public void Validate_WhenTokenLifetimesAreInsideTheRange_ShouldSucceed(string lifetime)
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings
        {
            Key = ValidKey,
            AccessTokenLifetime = TimeSpan.Parse(lifetime),
            BackgroundTokenLifetime = TimeSpan.Parse(lifetime)
        });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase("00:00:59")]
    [TestCase("00:00:00")]
    [TestCase("-00:05:00")]
    [TestCase("7.00:00:01")]
    public void Validate_WhenTokenLifetimesAreOutsideTheRange_ShouldFailForBothKeys(string lifetime)
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings
        {
            Key = ValidKey,
            AccessTokenLifetime = TimeSpan.Parse(lifetime),
            BackgroundTokenLifetime = TimeSpan.Parse(lifetime)
        });

        // Assert
        result.Failures.Should().HaveCount(2);
        result.FailureMessage.Should().Contain("JwtSettings:AccessTokenLifetime").And.Contain("JwtSettings:BackgroundTokenLifetime");
    }

    [TestCase("01:00:00", true)]
    [TestCase("365.00:00:00", true)]
    [TestCase("00:59:59", false)]
    [TestCase("365.00:00:01", false)]
    public void Validate_WhenRefreshIdleLifetimeIsChecked_ShouldFollowTheRange(string lifetime, bool valid)
    {
        // Act: the absolute lifetime stays at its limit so only the idle rule can fail
        var result = _validator.Validate(null, new JwtSettings
        {
            Key = ValidKey,
            RefreshIdleLifetime = TimeSpan.Parse(lifetime),
            RefreshAbsoluteLifetime = TimeSpan.FromDays(365)
        });

        // Assert
        result.Succeeded.Should().Be(valid);
        if (!valid)
        {
            result.FailureMessage.Should().Contain("JwtSettings:RefreshIdleLifetime");
        }
    }

    [Test]
    public void Validate_WhenAbsoluteLifetimeIsShorterThanIdleLifetime_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings
        {
            Key = ValidKey,
            RefreshIdleLifetime = TimeSpan.FromDays(30),
            RefreshAbsoluteLifetime = TimeSpan.FromDays(29)
        });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("JwtSettings:RefreshAbsoluteLifetime");
    }

    [Test]
    public void Validate_WhenAbsoluteLifetimeEqualsIdleLifetime_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings
        {
            Key = ValidKey,
            RefreshIdleLifetime = TimeSpan.FromDays(30),
            RefreshAbsoluteLifetime = TimeSpan.FromDays(30)
        });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenAbsoluteLifetimeIsAboveTheMaximum_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings
        {
            Key = ValidKey,
            RefreshAbsoluteLifetime = TimeSpan.FromDays(366)
        });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("JwtSettings:RefreshAbsoluteLifetime");
    }

    [TestCase("00:00:00", true)]
    [TestCase("00:05:00", true)]
    [TestCase("00:05:01", false)]
    [TestCase("-00:00:01", false)]
    public void Validate_WhenReuseGraceIsChecked_ShouldFollowTheRange(string grace, bool valid)
    {
        // Act
        var result = _validator.Validate(null, new JwtSettings { Key = ValidKey, RefreshReuseGrace = TimeSpan.Parse(grace) });

        // Assert
        result.Succeeded.Should().Be(valid);
        if (!valid)
        {
            result.FailureMessage.Should().Contain("JwtSettings:RefreshReuseGrace");
        }
    }
}
