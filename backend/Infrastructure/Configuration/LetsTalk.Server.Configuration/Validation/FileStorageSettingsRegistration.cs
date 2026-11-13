using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Validation;

public static class FileStorageSettingsRegistration
{
    /// <summary>
    /// Registers and validates <see cref="FileStorageSettings"/>. <paramref name="requireBasePath"/> is true in a host
    /// that stores files on disk: it then does not start without <c>FileStorage:BasePath</c>.
    /// </summary>
    public static IServiceCollection AddFileStorageSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        bool requireBasePath)
    {
        services.AddValidatedSettings<FileStorageSettings, FileStorageSettingsValidator>(
            configuration, FileStorageSettingsValidator.Section);

        if (requireBasePath)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IValidateOptions<FileStorageSettings>, FileStorageBasePathValidator>());
        }

        return services;
    }
}
