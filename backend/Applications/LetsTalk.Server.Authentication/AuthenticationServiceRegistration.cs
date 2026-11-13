using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Services.Cache.LoginCodes;
using LetsTalk.Server.Authentication.Services.Cache.Token;
using LetsTalk.Server.Authentication.Services.Session;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;
using LetsTalk.Server.Logging;
using LetsTalk.Server.Persistence.Redis;

namespace LetsTalk.Server.Authentication
{
    public static class AuthenticationServiceRegistration
    {
        public static IServiceCollection AddAuthenticationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("all", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            services.AddGrpc();
            services.AddGrpcReflection();
            services.AddScoped<IJwtStorageService, JwtStorageService>();
            services.AddScoped<ILoginCodeGenerator, LoginCodeGenerator>();
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddMemoryCache();
            services.AddSingleton(TimeProvider.System);
            services.AddLoggingServices();
            services.AddValidatedSettings<CachingSettings, CachingSettingsValidator>(configuration, CachingSettingsValidator.Section);
            switch (configuration.GetValue<string>("Features:CachingMode"))
            {
                case "redis":
                    services.AddRedisCache();
                    services.AddScoped<IJwtCacheService, RedisCacheTokenService>();
                    services.AddScoped<IRefreshSessionService, RedisRefreshSessionService>();
                    services.AddScoped<ILoginCodeCacheService, LoginCodeRedisCacheService>();
                    break;
                default:
                    services.AddMemoryCache();
                    services.AddScoped<IJwtCacheService, MemoryCacheTokenService>();
                    services.AddScoped<IRefreshSessionService, MemoryRefreshSessionService>();
                    services.AddScoped<ILoginCodeCacheService, LoginCodeMemoryCacheService>();
                    break;
            }

            return services;
        }
    }
}
