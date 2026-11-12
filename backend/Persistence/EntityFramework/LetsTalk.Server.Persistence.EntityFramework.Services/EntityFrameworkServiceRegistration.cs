using LetsTalk.Server.Persistence.AgnosticServices.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using LetsTalk.Server.Persistence.EntityFramework.Repository;

namespace LetsTalk.Server.Persistence.EntityFramework.Services;

public static class EntityFrameworkServiceRegistration
{
    public static IServiceCollection AddEntityFrameworkServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMessageAgnosticService, MessageEntityFrameworkService>();
        services.AddScoped<IAccountAgnosticService, AccountEntityFrameworkService>();
        services.AddScoped<IProfileAgnosticService, ProfileEntityFrameworkService>();
        services.AddScoped<IChatAgnosticService, ChatEntityFrameworkService>();
        services.AddScoped<ILinkPreviewAgnosticService, LinkPreviewEntityFrameworkService>();
        services.AddPersistenceRepositoryServices(configuration);

        return services;
    }
}
