using LetsTalk.Server.Persistence.AgnosticServices.Abstractions;
using LetsTalk.Server.Persistence.CosmosDB.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LetsTalk.Server.Persistence.CosmosDB.Services;

public static class CosmosDBServicesRegistration
{
    public static async Task<IServiceCollection> AddCosmosDBServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMessageAgnosticService, MessageCosmosDBService>();
        services.AddScoped<IAccountAgnosticService, AccountCosmosDBService>();
        services.AddScoped<IProfileAgnosticService, ProfileCosmosDBService>();
        services.AddScoped<IChatAgnosticService, ChatCosmosDBService>();
        services.AddScoped<ILinkPreviewAgnosticService, LinkPreviewCosmosDBService>();
        services.AddAutoMapper(
            cfg =>
            {
                cfg.LicenseKey = configuration.GetValue<string>("AutoMapper:LicenseKey");
            },
            Assembly.GetExecutingAssembly());

        await services.AddCosmosDBRepository(configuration);

        return services;
    }
}
