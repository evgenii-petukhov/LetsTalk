using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LetsTalk.Server.Persistence.MongoDB.Services;
using LetsTalk.Server.Persistence.EntityFramework.Services;
using LetsTalk.Server.Persistence.CosmosDB.Services;

namespace LetsTalk.Server.Persistence.AgnosticServices;

public static class PersistenceAgnosticServicesRegistration
{
    public static async Task<IServiceCollection> AddPersistenceAgnosticServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        switch (configuration.GetValue<string>("Features:DatabaseMode"))
        {
            case "MongoDB":
                await services.AddMongoDBServices(configuration);
                break;
            case "CosmosDB":
                await services.AddCosmosDBServices(configuration);
                break;
            default:
                services.AddEntityFrameworkServices(configuration);
                break;
        }

        return services;
    }
}
