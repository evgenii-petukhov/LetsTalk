namespace LetsTalk.Server.Configuration.Validation;

/// <summary>
/// The rules that the settings validators share. 
/// Each method returns an error message, or null when the value is fine.
/// </summary>
public static class SettingsRules
{
    public static string? Range(string key, int value, int min, int max)
    {
        return value >= min && value <= max
            ? null
            : $"{key} must be between {min} and {max}, but is {value}.";
    }

    public static string? Range(string key, TimeSpan value, TimeSpan min, TimeSpan max)
    {
        return value >= min && value <= max
            ? null
            : $"{key} must be between {min} and {max}, but is {value}.";
    }

    /// <summary>For required values, such as secrets. The message does not contain the value.</summary>
    public static string? NotEmpty(string key, string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? $"{key} must be set." : null;
    }
}
