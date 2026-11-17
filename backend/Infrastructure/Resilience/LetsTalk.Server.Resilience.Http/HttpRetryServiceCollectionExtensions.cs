using LetsTalk.Server.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Resilience;

public static class HttpRetryServiceCollectionExtensions
{
    /// <summary>
    /// Binds the <c>Resilience:Http</c> section to <see cref="HttpRetrySettings"/> and checks it when the service
    /// starts (spec 005 and 006). Every client that uses <see cref="AddHttpRetry"/> reads these numbers.
    /// </summary>
    public static IServiceCollection AddHttpRetrySettings(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddValidatedSettings<HttpRetrySettings, HttpRetrySettingsValidator>(
            configuration, HttpRetrySettingsValidator.Section);
    }

    /// <summary>
    /// Repeats the failed calls of this client by the rules of <see cref="HttpRetryPipeline"/>. <paramref name="target"/>
    /// names the client in the warning that each repeat writes. Needs <see cref="AddHttpRetrySettings"/>.
    /// </summary>
    public static IHttpClientBuilder AddHttpRetry(this IHttpClientBuilder builder, string target)
    {
        builder.AddResilienceHandler("retry", (pipeline, context) =>
        {
            var settings = context.ServiceProvider.GetRequiredService<IOptions<HttpRetrySettings>>().Value;
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(HttpRetryPipeline).FullName!);
            HttpRetryPipeline.Configure(pipeline, settings, retry =>
                HttpRetryLog.Retrying(logger, target, retry.Attempt, retry.Reason, retry.Delay.TotalSeconds));
        });

        return builder;
    }
}
