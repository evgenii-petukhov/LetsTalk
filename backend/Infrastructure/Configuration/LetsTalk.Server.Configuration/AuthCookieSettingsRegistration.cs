using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Configuration;

public static class AuthCookieSettingsRegistration
{
    public static IServiceCollection AddAuthCookieSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddValidatedSettings<AuthCookieSettings, AuthCookieSettingsValidator>(
            configuration, AuthCookieSettingsValidator.Section);
    }
}
