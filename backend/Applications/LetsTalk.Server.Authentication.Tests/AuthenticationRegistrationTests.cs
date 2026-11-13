using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class AuthenticationRegistrationTests
{
    [Test]
    public void AddAuthenticationServices_WhenKeyIsShort_ShouldFailWhenTheOptionsAreRead()
    {
        // Arrange
        using var provider = Register("short-key");

        // Act
        var act = () => provider.GetRequiredService<IOptions<JwtSettings>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*JwtSettings:Key must be at least 32 bytes*")
            .And.Message.Should().NotContain("short-key");
    }

    [Test]
    public void AddAuthenticationServices_WhenKeyIsValid_ShouldResolveTheSettingsWithTheDefaultLifetimes()
    {
        // Arrange
        using var provider = Register("0123456789abcdef0123456789abcdef");

        // Act
        var settings = provider.GetRequiredService<IOptions<JwtSettings>>().Value;

        // Assert
        settings.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(15));
    }

    private static ServiceProvider Register(string key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JwtSettings:Key"] = key })
            .Build();

        return new ServiceCollection()
            .AddAuthenticationServices(configuration)
            .BuildServiceProvider();
    }
}
