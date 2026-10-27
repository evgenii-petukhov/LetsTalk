using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class AuthCookieSettingsTests
{
    private const string SectionName = "AuthCookie";
    private const string DefaultName = "auth-token";
    private const string DefaultSameSite = "Strict";
    private const string CustomName = "session";
    private const string CustomDomain = "example.com";
    private const string CustomSameSite = "Lax";

    [Test]
    public void AuthCookieSettings_WhenSectionMissing_ShouldUseDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var settings = new AuthCookieSettings();

        // Act
        configuration.GetSection(SectionName).Bind(settings);

        // Assert
        settings.Name.Should().Be(DefaultName);
        settings.Domain.Should().BeEmpty();
        settings.Secure.Should().BeTrue();
        settings.SameSite.Should().Be(DefaultSameSite);
    }

    [Test]
    public void AuthCookieSettings_WhenSectionPresent_ShouldBindValues()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SectionName}:Name"] = CustomName,
                [$"{SectionName}:Domain"] = CustomDomain,
                [$"{SectionName}:Secure"] = "false",
                [$"{SectionName}:SameSite"] = CustomSameSite
            })
            .Build();
        var settings = new AuthCookieSettings();

        // Act
        configuration.GetSection(SectionName).Bind(settings);

        // Assert
        settings.Name.Should().Be(CustomName);
        settings.Domain.Should().Be(CustomDomain);
        settings.Secure.Should().BeFalse();
        settings.SameSite.Should().Be(CustomSameSite);
    }
}
