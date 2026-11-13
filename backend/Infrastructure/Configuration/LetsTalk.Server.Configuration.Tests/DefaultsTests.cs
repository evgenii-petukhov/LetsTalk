using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Tests;

/// <summary>The defaults of the classes equal the values shipped in appsettings.json (plan D2).</summary>
[TestFixture]
public class DefaultsTests
{
    [Test]
    public void MessagingSettings_ShouldDefaultToTheShippedValue()
    {
        new MessagingSettings().MessagesPerPage.Should().Be(20);
    }

    [Test]
    public void SecuritySettings_ShouldDefaultToTheShippedValue()
    {
        new SecuritySettings().AntiSpamTokenLifeTimeInSeconds.Should().Be(60);
    }

    [Test]
    public void CloudflareSettings_ShouldDefaultTheTokenTtlAndLeaveTheSecretsEmpty()
    {
        var settings = new CloudflareSettings();

        settings.TokenTtl.Should().Be(86400);
        settings.TurnTokenId.Should().BeNull();
        settings.ApiToken.Should().BeNull();
    }

    [Test]
    public void CachingSettings_ShouldDefaultToTheShippedValues()
    {
        new CachingSettings().Should().BeEquivalentTo(new
        {
            MessagesCacheLifeTimeInSeconds = 120,
            ChatCacheLifeTimeInSeconds = 30,
            AccountCacheLifeTimeInSeconds = 30,
            ProfileCacheLifeTimeInSeconds = 30,
            ImagesCacheLifeTimeInSeconds = 86400,
            ImageSizeThresholdInBytes = 102400,
            LoginCodeCacheLifeTimeInSeconds = 120
        });
    }

    [Test]
    public void ImageConstraints_ShouldDefaultToTheShippedValues()
    {
        new ImageConstraints().Should().BeEquivalentTo(new
        {
            ImagePreviewMaxWidth = 512,
            ImagePreviewMaxHeight = 512,
            AvatarMaxWidth = 512,
            AvatarMaxHeight = 512,
            PictureMaxWidth = 4000,
            PictureMaxHeight = 4000
        });
    }

    [Test]
    public void Messaging_WhenTheSectionIsAbsent_ShouldKeepTheDefault()
    {
        // Act
        var options = Resolve<MessagingSettings, MessagingSettingsValidator>(
            MessagingSettingsValidator.Section, []);

        // Assert
        options.MessagesPerPage.Should().Be(20);
    }

    [Test]
    public void Caching_WhenOneKeyIsAbsent_ShouldKeepTheDefaultAndTheOtherKeysWin()
    {
        // Act
        var options = Resolve<CachingSettings, CachingSettingsValidator>(
            CachingSettingsValidator.Section,
            new() { ["Caching:ChatCacheLifeTimeInSeconds"] = "0" });

        // Assert
        options.ChatCacheLifeTimeInSeconds.Should().Be(0);
        options.MessagesCacheLifeTimeInSeconds.Should().Be(120);
    }

    private static TSettings Resolve<TSettings, TValidator>(string section, Dictionary<string, string?> values)
        where TSettings : class
        where TValidator : class, IValidateOptions<TSettings>
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using var provider = new ServiceCollection()
            .AddValidatedSettings<TSettings, TValidator>(configuration, section)
            .BuildServiceProvider();
        return provider.GetRequiredService<IOptions<TSettings>>().Value;
    }
}
