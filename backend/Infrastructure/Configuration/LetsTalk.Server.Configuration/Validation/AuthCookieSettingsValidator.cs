using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Configuration.Validation;

public class AuthCookieSettingsValidator : SettingsValidatorBase<AuthCookieSettings>
{
    public const string Section = "AuthCookie";

    private static readonly string[] SameSiteValues = ["None", "Lax", "Strict"];

    protected override string SectionName => Section;

    protected override IEnumerable<string?> GetErrors(AuthCookieSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            yield return $"{Key(nameof(settings.Name))} must not be empty.";
        }

        if (string.IsNullOrWhiteSpace(settings.RefreshName))
        {
            yield return $"{Key(nameof(settings.RefreshName))} must not be empty.";
        }
        else if (string.Equals(settings.RefreshName, settings.Name, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{Key(nameof(settings.RefreshName))} must differ from {nameof(settings.Name)}, otherwise the two cookies overwrite each other.";
        }

        if (string.IsNullOrWhiteSpace(settings.RefreshPath)
            || !settings.RefreshPath.StartsWith('/')
            || settings.RefreshPath.Any(c => c == ';' || char.IsWhiteSpace(c)))
        {
            yield return $"{Key(nameof(settings.RefreshPath))} must be an absolute path such as /api/Authentication, but is '{settings.RefreshPath}'.";
        }

        var sameSite = SameSiteValues.FirstOrDefault(x =>
            string.Equals(x, settings.SameSite, StringComparison.OrdinalIgnoreCase));
        if (sameSite == null)
        {
            yield return $"{Key(nameof(settings.SameSite))} must be one of {string.Join(", ", SameSiteValues)}, but is '{settings.SameSite}'.";
        }
        else if (sameSite == "None" && !settings.Secure)
        {
            yield return $"{Key(nameof(settings.SameSite))} None requires {nameof(settings.Secure)} to be true, otherwise browsers reject the cookie.";
        }

        if (!string.IsNullOrEmpty(settings.Domain)
            && settings.Domain.Any(c => c is ':' or '/' or '\\' || char.IsWhiteSpace(c)))
        {
            yield return $"{Key(nameof(settings.Domain))} must be a host name without scheme, port or path, but is '{settings.Domain}'.";
        }
    }
}
