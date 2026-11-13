using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class ImageConstraintsValidatorTests
{
    private readonly ImageConstraintsValidator _validator = new();

    private static readonly string[] Properties =
    [
        nameof(ImageConstraints.ImagePreviewMaxWidth),
        nameof(ImageConstraints.ImagePreviewMaxHeight),
        nameof(ImageConstraints.AvatarMaxWidth),
        nameof(ImageConstraints.AvatarMaxHeight),
        nameof(ImageConstraints.PictureMaxWidth),
        nameof(ImageConstraints.PictureMaxHeight)
    ];

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new ImageConstraints());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenValueIsInsideTheRange_ShouldSucceed(
        [ValueSource(nameof(Properties))] string property,
        [Values(1, 16383)] int value)
    {
        // Arrange: keep the avatar limit not above the picture limit
        var settings = With(property, value);
        settings.AvatarMaxWidth = Math.Min(settings.AvatarMaxWidth, settings.PictureMaxWidth);
        settings.AvatarMaxHeight = Math.Min(settings.AvatarMaxHeight, settings.PictureMaxHeight);

        // Act
        var result = _validator.Validate(null, settings);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void Validate_WhenValueIsOutsideTheRange_ShouldFailAndNameTheKey(
        [ValueSource(nameof(Properties))] string property,
        [Values(0, 16384)] int value)
    {
        // Act
        var result = _validator.Validate(null, With(property, value));

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain($"ImageConstraints:{property} must be between 1 and 16383");
    }

    [Test]
    public void Validate_WhenAvatarWidthIsLargerThanPictureWidth_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new ImageConstraints { AvatarMaxWidth = 5000, PictureMaxWidth = 4000 });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ImageConstraints:AvatarMaxWidth").And.Contain("PictureMaxWidth");
    }

    [Test]
    public void Validate_WhenAvatarHeightIsLargerThanPictureHeight_ShouldFail()
    {
        // Act
        var result = _validator.Validate(null, new ImageConstraints { AvatarMaxHeight = 5000, PictureMaxHeight = 4000 });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ImageConstraints:AvatarMaxHeight").And.Contain("PictureMaxHeight");
    }

    private static ImageConstraints With(string property, int value)
    {
        var settings = new ImageConstraints();
        typeof(ImageConstraints).GetProperty(property)!.SetValue(settings, value);
        return settings;
    }
}
