using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class ImageConstraintsValidator : SettingsValidatorBase<ImageConstraints>
{
    public const string Section = "ImageConstraints";
    public const int MinSize = 1;
    // The largest side WebP, the only accepted format, can hold
    public const int MaxSize = 16383;

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(ImageConstraints settings)
    {
        yield return Size(nameof(settings.ImagePreviewMaxWidth), settings.ImagePreviewMaxWidth);
        yield return Size(nameof(settings.ImagePreviewMaxHeight), settings.ImagePreviewMaxHeight);
        yield return Size(nameof(settings.AvatarMaxWidth), settings.AvatarMaxWidth);
        yield return Size(nameof(settings.AvatarMaxHeight), settings.AvatarMaxHeight);
        yield return Size(nameof(settings.PictureMaxWidth), settings.PictureMaxWidth);
        yield return Size(nameof(settings.PictureMaxHeight), settings.PictureMaxHeight);

        if (settings.AvatarMaxWidth > settings.PictureMaxWidth)
        {
            yield return $"{Key(nameof(settings.AvatarMaxWidth))} must not be larger than {Key(nameof(settings.PictureMaxWidth))}.";
        }

        if (settings.AvatarMaxHeight > settings.PictureMaxHeight)
        {
            yield return $"{Key(nameof(settings.AvatarMaxHeight))} must not be larger than {Key(nameof(settings.PictureMaxHeight))}.";
        }
    }

    private string? Size(string property, int value) => SettingsRules.Range(Key(property), value, MinSize, MaxSize);
}
