using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Validation;

public static class ValidatedSettingsServiceCollectionExtensions
{
    /// <summary>
    /// Binds a configuration section to <typeparamref name="TSettings"/> and checks it with
    /// <typeparamref name="TValidator"/> when the service starts: a wrong configuration stops the start and
    /// the message lists every broken rule. Calling it twice for one class does not repeat a failure.
    /// </summary>
    public static IServiceCollection AddValidatedSettings<TSettings, TValidator>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TSettings : class
        where TValidator : class, IValidateOptions<TSettings>
    {
        services.AddOptions<TSettings>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TSettings>, TValidator>());
        return services;
    }
}
