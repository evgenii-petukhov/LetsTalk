using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using LetsTalk.Server.SignPackage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.SignPackage;

public static class SignPackageServiceCollectionExtensions
{
    public static IServiceCollection AddSignPackageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedSettings<SignPackageSettings, SignPackageSettingsValidator>(configuration, SignPackageSettingsValidator.Section);
        services.AddScoped<ISignPackageService, SignPackageService>();

        return services;
    }
}
