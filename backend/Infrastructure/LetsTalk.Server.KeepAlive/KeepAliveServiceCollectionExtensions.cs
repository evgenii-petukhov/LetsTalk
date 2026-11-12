using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LetsTalk.Server.KeepAlive;

public static class KeepAliveServiceCollectionExtensions
{
    // Registers the hosted service and its settings. The stores register their own IKeepAliveTarget,
    // so only the stores that are actually configured are signalled. Call this only in the service
    // that should send the signals.
    public static IServiceCollection AddKeepAlive(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KeepAliveSettings>(configuration.GetSection("KeepAlive"));
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<KeepAliveService>();

        return services;
    }
}
