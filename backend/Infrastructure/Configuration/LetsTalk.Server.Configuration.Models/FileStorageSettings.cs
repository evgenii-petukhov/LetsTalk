namespace LetsTalk.Server.Configuration.Models;

public class FileStorageSettings
{
    /// <summary>The folder that holds the files. A deployment value: no default, and not listed in appsettings.json.</summary>
    public string? BasePath { get; set; }

    /// <summary>A folder name relative to <see cref="BasePath"/>, without '..'.</summary>
    public string? ImageFolder { get; set; } = "Images";
}
