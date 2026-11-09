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
    private const string DefaultRefreshName = "auth-refresh";
    private const string DefaultRefreshPath = "/api/Authentication";
    private const string CustomName = "session";
    private const string CustomDomain = "example.com";
    private const string CustomSameSite = "Lax";
    private const string CustomRefreshName = "session-refresh";
    private const string CustomRefreshPath = "/auth";

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
        settings.RefreshName.Should().Be(DefaultRefreshName);
        settings.RefreshPath.Should().Be(DefaultRefreshPath);
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
                [$"{SectionName}:SameSite"] = CustomSameSite,
                [$"{SectionName}:RefreshName"] = CustomRefreshName,
                [$"{SectionName}:RefreshPath"] = CustomRefreshPath
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
        settings.RefreshName.Should().Be(CustomRefreshName);
        settings.RefreshPath.Should().Be(CustomRefreshPath);
    }
}
