using LetsTalk.Server.KeepAlive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LetsTalk.Server.Persistence.Redis
{
    public static class RedisRegistration
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services)
        {
            services.AddSingleton<RedisConnection>();
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<IKeepAliveTarget, RedisKeepAliveTarget>();

            return services;
        }
    }
}
