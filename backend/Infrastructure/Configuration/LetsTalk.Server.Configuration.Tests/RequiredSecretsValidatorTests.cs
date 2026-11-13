using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Tests;

/// <summary>Required values with no default: a missing one stops the start, and no message shows a value (FR-014).</summary>
[TestFixture]
public class RequiredSecretsValidatorTests
{
    private const string Secret = "do-not-print-me";

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void SignPackage_WhenPepperIsEmpty_ShouldFail(string? pepper)
    {
        // Act
        var result = new SignPackageSettingsValidator().Validate(null, new SignPackageSettings { Pepper = pepper });

        // Assert
        result.Failures.Should().ContainSingle().Which.Should().Be("SignPackage:Pepper must be set.");
    }

    [Test]
    public void SignPackage_WhenPepperIsSet_ShouldSucceed()
    {
        new SignPackageSettingsValidator().Validate(null, new SignPackageSettings { Pepper = Secret })
            .Succeeded.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void LinkPreview_WhenSecretKeyIsEmpty_ShouldFail(string? key)
    {
        // Act
        var result = new LinkPreviewSettingsValidator().Validate(null, new LinkPreviewSettings { SecretKey = key });

        // Assert
        result.Failures.Should().ContainSingle().Which.Should().Be("LinkPreview:SecretKey must be set.");
    }

    [Test]
    public void LinkPreview_WhenSecretKeyIsSet_ShouldSucceed()
    {
        new LinkPreviewSettingsValidator().Validate(null, new LinkPreviewSettings { SecretKey = Secret })
            .Succeeded.Should().BeTrue();
    }

    [Test]
    public void Aws_WhenAllFourValuesAreSet_ShouldSucceed()
    {
        new AwsSettingsValidator().Validate(null, ValidAws()).Succeeded.Should().BeTrue();
    }

    [TestCase(nameof(AwsSettings.AccessKey))]
    [TestCase(nameof(AwsSettings.SecretKey))]
    [TestCase(nameof(AwsSettings.Region))]
    [TestCase(nameof(AwsSettings.BucketName))]
    public void Aws_WhenOneValueIsEmpty_ShouldFailAndNameOnlyThatKey(string property)
    {
        // Arrange
        var settings = ValidAws();
        typeof(AwsSettings).GetProperty(property)!.SetValue(settings, " ");

        // Act
        var result = new AwsSettingsValidator().Validate(null, settings);

        // Assert
        result.Failures.Should().ContainSingle().Which.Should().Be($"Aws:{property} must be set.");
    }

    [Test]
    public void Aws_WhenNothingIsSet_ShouldListAllFourKeys()
    {
        new AwsSettingsValidator().Validate(null, new AwsSettings()).Failures.Should().HaveCount(4);
    }

    [Test]
    public void EmailService_WhenEverythingIsSet_ShouldSucceed()
    {
        new EmailServiceSettingsValidator().Validate(null, ValidEmail()).Succeeded.Should().BeTrue();
    }

    [TestCase(nameof(EmailServiceSettings.SenderEmail))]
    [TestCase(nameof(EmailServiceSettings.SenderName))]
    [TestCase(nameof(EmailServiceSettings.Server))]
    [TestCase(nameof(EmailServiceSettings.UserName))]
    [TestCase(nameof(EmailServiceSettings.Password))]
    public void EmailService_WhenOneTextIsEmpty_ShouldFailAndNameOnlyThatKey(string property)
    {
        // Arrange
        var settings = ValidEmail();
        typeof(EmailServiceSettings).GetProperty(property)!.SetValue(settings, "");

        // Act
        var result = new EmailServiceSettingsValidator().Validate(null, settings);

        // Assert
        result.Failures.Should().ContainSingle().Which.Should().Be($"EmailService:{property} must be set.");
    }

    [TestCase(1)]
    [TestCase(25)]
    [TestCase(587)]
    [TestCase(65535)]
    public void EmailService_WhenPortIsInsideTheRange_ShouldSucceed(int port)
    {
        var settings = ValidEmail();
        settings.Port = port;

        new EmailServiceSettingsValidator().Validate(null, settings).Succeeded.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void EmailService_WhenPortIsOutsideTheRange_ShouldFail(int port)
    {
        var settings = ValidEmail();
        settings.Port = port;

        var result = new EmailServiceSettingsValidator().Validate(null, settings);

        result.Failures.Should().ContainSingle().Which.Should().Contain("EmailService:Port must be between 1 and 65535");
    }

    [Test]
    public void AwsRegisteredTwice_WhenValuesAreMissing_ShouldReportEachFailureOnce()
    {
        // Arrange: the API registers Aws in two modes (link preview and image processing)
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddValidatedSettings<AwsSettings, AwsSettingsValidator>(configuration, AwsSettingsValidator.Section);
        services.AddValidatedSettings<AwsSettings, AwsSettingsValidator>(configuration, AwsSettingsValidator.Section);
        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<AwsSettings>>().Value;

        // Assert
        var message = act.Should().Throw<OptionsValidationException>().Which.Message;
        message.Split("Aws:AccessKey must be set.").Should().HaveCount(2);
    }

    private static AwsSettings ValidAws() => new()
    {
        AccessKey = Secret,
        SecretKey = Secret,
        Region = "eu-central-1",
        BucketName = "bucket"
    };

    private static EmailServiceSettings ValidEmail() => new()
    {
        SenderEmail = "a@example.com",
        SenderName = "Sender",
        Server = "smtp.example.com",
        Port = 587,
        UserName = "user",
        Password = Secret
    };
}
