using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class AuthCookieSettingsValidatorTests
{
    private const string SectionName = "AuthCookie";

    private readonly AuthCookieSettingsValidator _validator = new();

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase("Strict")]
    [TestCase("lax")]
    public void Validate_WhenSameSiteIsKnown_ShouldSucceed(string sameSite)
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { SameSite = sameSite });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase("Strictt")]
    [TestCase("")]
    [TestCase("Unspecified")]
    public void Validate_WhenSameSiteIsUnknown_ShouldFail(string sameSite)
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { SameSite = sameSite });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AuthCookie:SameSite");
    }

    [Test]
    public void Validate_WhenSameSiteIsNoneAndNotSecure_ShouldFail()
    {
        // Act: browsers reject SameSite=None without Secure, so sign-in would silently store no cookie
        var result = _validator.Validate(null, new AuthCookieSettings { SameSite = "None", Secure = false });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AuthCookie:SameSite None requires Secure");
    }

    [Test]
    public void Validate_WhenSameSiteIsNoneAndSecure_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { SameSite = "None", Secure = true });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenNameIsEmpty_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { Name = " " });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AuthCookie:Name must not be empty");
    }

    [Test]
    public void Validate_WhenRefreshNameIsEmpty_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { RefreshName = " " });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AuthCookie:RefreshName must not be empty");
    }

    [Test]
    public void Validate_WhenRefreshNameEqualsName_ShouldFail()
    {
        // Act: two cookies with one name would overwrite each other
        var result = _validator.Validate(null, new AuthCookieSettings { Name = "session", RefreshName = "SESSION" });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AuthCookie:RefreshName must differ from Name");
    }

    [TestCase("/api/Authentication")]
    [TestCase("/")]
    public void Validate_WhenRefreshPathIsAbsolute_ShouldSucceed(string path)
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { RefreshPath = path });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase("")]
    [TestCase("api/Authentication")]
    [TestCase("/api;Authentication")]
    [TestCase("/api Authentication")]
    public void Validate_WhenRefreshPathIsNotAnAbsolutePath_ShouldFail(string path)
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { RefreshPath = path });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AuthCookie:RefreshPath");
    }

    [TestCase("example.com")]
    [TestCase(".example.com")]
    public void Validate_WhenDomainIsAHostName_ShouldSucceed(string domain)
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { Domain = domain });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase("https://example.com")]
    [TestCase("example.com:443")]
    [TestCase("example.com/")]
    [TestCase("example.com\\path")]
    [TestCase("petukhov fyi")]
    public void Validate_WhenDomainIsNotAHostName_ShouldFail(string domain)
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { Domain = domain });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AuthCookie:Domain");
    }

    [Test]
    public void Validate_WhenSeveralValuesAreWrong_ShouldListThemAll()
    {
        // Act
        var result = _validator.Validate(null, new AuthCookieSettings { Name = "", SameSite = "Nope", Domain = "a/b" });

        // Assert
        result.Failures.Should().HaveCount(3);
    }

    [Test]
    public void AddAuthCookieSettings_ShouldRegisterTheOptionsAndTheValidator()
    {
        // Act
        var services = Register(("Domain", "example.com"));
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IOptions<AuthCookieSettings>>().Value.Domain.Should().Be("example.com");
        provider.GetServices<IValidateOptions<AuthCookieSettings>>().Should().ContainSingle();
    }

    [Test]
    public void AddAuthCookieSettings_WhenSectionIsInvalid_ShouldFailWhenTheOptionsAreRead()
    {
        // Arrange
        using var provider = Register(("SameSite", "Strictt")).BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<AuthCookieSettings>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*AuthCookie:SameSite*");
    }

    private static IServiceCollection Register(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(x =>
                new KeyValuePair<string, string?>($"{SectionName}:{x.Key}", x.Value)))
            .Build();

        return new ServiceCollection().AddAuthCookieSettings(configuration);
    }
}
