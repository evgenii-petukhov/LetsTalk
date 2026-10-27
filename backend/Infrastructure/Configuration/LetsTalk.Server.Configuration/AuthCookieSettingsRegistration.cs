using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Configuration;

public static class AuthCookieSettingsRegistration
{
    private const string SectionName = "AuthCookie";

    private static readonly string[] SameSiteValues = ["None", "Lax", "Strict"];

    // A wrong cookie setting does not fail loudly later: the browser silently drops the cookie and
    // every request ends with 401. The settings are therefore checked once, when the service starts.
    public static IServiceCollection AddAuthCookieSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var settings = new AuthCookieSettings();
        section.Bind(settings);

        var errors = GetErrors(settings).ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"The {SectionName} settings are invalid: {string.Join(" ", errors)}");
        }

        return services.Configure<AuthCookieSettings>(section);
    }

    private static IEnumerable<string> GetErrors(AuthCookieSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            yield return "Name must not be empty.";
        }

        var sameSite = SameSiteValues.FirstOrDefault(x =>
            string.Equals(x, settings.SameSite, StringComparison.OrdinalIgnoreCase));
        if (sameSite == null)
        {
            yield return $"SameSite must be one of {string.Join(", ", SameSiteValues)}, but is '{settings.SameSite}'.";
        }
        else if (sameSite == "None" && !settings.Secure)
        {
            yield return "SameSite None requires Secure to be true, otherwise browsers reject the cookie.";
        }

        if (!string.IsNullOrEmpty(settings.Domain)
            && settings.Domain.Any(c => c is ':' or '/' or '\\' || char.IsWhiteSpace(c)))
        {
            yield return $"Domain must be a host name without scheme, port or path, but is '{settings.Domain}'.";
        }
    }
}
