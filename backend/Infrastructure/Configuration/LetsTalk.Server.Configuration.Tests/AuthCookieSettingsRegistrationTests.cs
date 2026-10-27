using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class AuthCookieSettingsRegistrationTests
{
    private const string SectionName = "AuthCookie";

    [Test]
    public void AddAuthCookieSettings_WhenSectionMissing_ShouldAcceptTheDefaults()
    {
        // Act
        var act = () => Register();

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void AddAuthCookieSettings_ShouldRegisterTheOptions()
    {
        // Act
        var services = Register(("Domain", "petukhov.fyi"));

        // Assert
        services.Should().Contain(x => x.ServiceType == typeof(IConfigureOptions<AuthCookieSettings>));
    }

    [TestCase("Strict")]
    [TestCase("lax")]
    public void AddAuthCookieSettings_WhenSameSiteIsKnown_ShouldNotThrow(string sameSite)
    {
        // Act
        var act = () => Register(("SameSite", sameSite));

        // Assert
        act.Should().NotThrow();
    }

    [TestCase("Strictt")]
    [TestCase("")]
    [TestCase("Unspecified")]
    public void AddAuthCookieSettings_WhenSameSiteIsUnknown_ShouldThrow(string sameSite)
    {
        // Act
        var act = () => Register(("SameSite", sameSite));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AuthCookie*SameSite*");
    }

    [Test]
    public void AddAuthCookieSettings_WhenSameSiteIsNoneAndNotSecure_ShouldThrow()
    {
        // Act: browsers reject SameSite=None without Secure, so sign-in would silently store no cookie
        var act = () => Register(("SameSite", "None"), ("Secure", "false"));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AuthCookie*None*Secure*");
    }

    [Test]
    public void AddAuthCookieSettings_WhenSameSiteIsNoneAndSecure_ShouldNotThrow()
    {
        // Act
        var act = () => Register(("SameSite", "None"), ("Secure", "true"));

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void AddAuthCookieSettings_WhenNameIsEmpty_ShouldThrow()
    {
        // Act
        var act = () => Register(("Name", " "));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AuthCookie*Name*");
    }

    [TestCase("petukhov.fyi")]
    [TestCase(".petukhov.fyi")]
    public void AddAuthCookieSettings_WhenDomainIsAHostName_ShouldNotThrow(string domain)
    {
        // Act
        var act = () => Register(("Domain", domain));

        // Assert
        act.Should().NotThrow();
    }

    [TestCase("https://petukhov.fyi")]
    [TestCase("petukhov.fyi:443")]
    [TestCase("petukhov.fyi/")]
    [TestCase("petukhov fyi")]
    public void AddAuthCookieSettings_WhenDomainIsNotAHostName_ShouldThrow(string domain)
    {
        // Act
        var act = () => Register(("Domain", domain));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AuthCookie*Domain*");
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
