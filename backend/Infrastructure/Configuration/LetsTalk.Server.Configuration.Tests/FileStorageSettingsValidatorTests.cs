using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class FileStorageSettingsValidatorTests
{
    private readonly FileStorageSettingsValidator _validator = new();
    private readonly FileStorageBasePathValidator _basePathValidator = new();

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new FileStorageSettings());

        // Assert
        result.Succeeded.Should().BeTrue();
        new FileStorageSettings().ImageFolder.Should().Be("Images");
    }

    [TestCase("Images")]
    [TestCase("a/b")]
    [TestCase("a\\b")]
    [TestCase("my.images")]
    public void Validate_WhenImageFolderIsARelativeName_ShouldSucceed(string folder)
    {
        // Act
        var result = _validator.Validate(null, new FileStorageSettings { ImageFolder = folder });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase("")]
    [TestCase("  ")]
    [TestCase(null)]
    [TestCase("/abs")]
    [TestCase("\\abs")]
    [TestCase("C:\\x")]
    [TestCase("..\\x")]
    [TestCase("a/../b")]
    [TestCase("..")]
    [TestCase("a<b")]
    [TestCase("a|b")]
    [TestCase("a?b")]
    public void Validate_WhenImageFolderIsNotARelativeName_ShouldFailAndNameTheKey(string? folder)
    {
        // Act
        var result = _validator.Validate(null, new FileStorageSettings { ImageFolder = folder });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("FileStorage:ImageFolder");
    }

    [Test]
    public void Validate_WhenBasePathIsMissingAndNotRequired_ShouldSucceed()
    {
        // Act: the first validator alone does not look at BasePath
        var result = _validator.Validate(null, new FileStorageSettings { BasePath = null });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ValidateBasePath_WhenBasePathIsEmpty_ShouldFail(string? basePath)
    {
        // Act
        var result = _basePathValidator.Validate(null, new FileStorageSettings { BasePath = basePath });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("FileStorage:BasePath must be set");
    }

    [Test]
    public void ValidateBasePath_WhenBasePathIsSet_ShouldSucceed()
    {
        // Act
        var result = _basePathValidator.Validate(null, new FileStorageSettings { BasePath = "C:\\data" });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void AddFileStorageSettings_WhenBasePathIsRequiredAndMissing_ShouldFailWhenTheOptionsAreRead()
    {
        // Arrange
        using var provider = Register(requireBasePath: true);

        // Act
        var act = () => provider.GetRequiredService<IOptions<FileStorageSettings>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*FileStorage:BasePath must be set*");
    }

    [Test]
    public void AddFileStorageSettings_WhenBasePathIsNotRequiredAndMissing_ShouldResolveTheSettings()
    {
        // Arrange
        using var provider = Register(requireBasePath: false);

        // Act
        var settings = provider.GetRequiredService<IOptions<FileStorageSettings>>().Value;

        // Assert
        settings.ImageFolder.Should().Be("Images");
        settings.BasePath.Should().BeNull();
    }

    [Test]
    public void AddFileStorageSettings_WhenBasePathIsRequiredAndSet_ShouldResolveTheSettings()
    {
        // Arrange
        using var provider = Register(requireBasePath: true, ("BasePath", "C:\\data"));

        // Act
        var settings = provider.GetRequiredService<IOptions<FileStorageSettings>>().Value;

        // Assert
        settings.BasePath.Should().Be("C:\\data");
    }

    private static ServiceProvider Register(bool requireBasePath, params (string Key, string Value)[] values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(x => new KeyValuePair<string, string?>($"FileStorage:{x.Key}", x.Value)))
            .Build();

        return new ServiceCollection()
            .AddFileStorageSettings(configuration, requireBasePath)
            .BuildServiceProvider();
    }
}
