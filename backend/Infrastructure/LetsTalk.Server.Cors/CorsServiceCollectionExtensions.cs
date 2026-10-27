using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LetsTalk.Server.Cors;

public static class CorsServiceCollectionExtensions
{
    public const string PolicyName = "all";

    // Credentialed requests (cookies) cannot be combined with "any origin", so the allowed
    // origins come from configuration. Outside Development an empty list fails at startup.
    public static IServiceCollection AddCredentialedCors(
        this IServiceCollection services,
        IConfiguration configuration,
        params string[] exposedHeaders)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IHostEnvironment>((options, environment) =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length == 0 && !environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must contain at least one origin outside the Development environment.");
            }

            options.AddPolicy(PolicyName, builder =>
            {
                builder
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();

                if (exposedHeaders.Length > 0)
                {
                    builder.WithExposedHeaders(exposedHeaders);
                }
            });
        });

        return services;
    }
}
