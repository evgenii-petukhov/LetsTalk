namespace LetsTalk.Server.Configuration.Models;

/// <remarks>Every limit is in pixels, from 1 to 16383 (the largest side WebP can hold). An avatar limit must not exceed the picture limit.</remarks>
public class ImageConstraints
{
    public int ImagePreviewMaxWidth { get; set; } = 512;

    public int ImagePreviewMaxHeight { get; set; } = 512;

    public int AvatarMaxWidth { get; set; } = 512;

    public int AvatarMaxHeight { get; set; } = 512;

    public int PictureMaxWidth { get; set; } = 4000;

    public int PictureMaxHeight { get; set; } = 4000;
}
