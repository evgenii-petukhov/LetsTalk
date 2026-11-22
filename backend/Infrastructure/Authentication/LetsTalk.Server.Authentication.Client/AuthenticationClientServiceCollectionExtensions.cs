using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static LetsTalk.Server.Authentication.JwtTokenGrpcService;
using static LetsTalk.Server.Authentication.LoginCodeGrpcService;
using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Authentication.Client;

public static class AuthenticationClientServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationClientServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IAuthenticationClient, AuthenticationClient>();
        services.AddValidatedSettings<GrpcRetrySettings, GrpcRetrySettingsValidator>(configuration, GrpcRetrySettingsValidator.Section);
        // Only the token client repeats calls; the login code client has no call that may be repeated (spec 006, FR-007)
        services.AddGrpcClient<JwtTokenGrpcServiceClient>(options => options.Address = new Uri(configuration.GetValue<string>("ApplicationUrls:Authentication")!))
            .ConfigureChannel((provider, channel) =>
                channel.ServiceConfig = GrpcRetryServiceConfig.Create(provider.GetRequiredService<IOptions<GrpcRetrySettings>>().Value));
        services.AddGrpcClient<LoginCodeGrpcServiceClient>(options => options.Address = new Uri(configuration.GetValue<string>("ApplicationUrls:Authentication")!));

        return services;
    }
}
