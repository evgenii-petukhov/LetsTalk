using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LetsTalk.Server.Persistence.MongoDB.Services;
using LetsTalk.Server.Persistence.EntityFramework.Services;
using LetsTalk.Server.Persistence.CosmosDB.Services;
using System.Reflection;

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

    // The assembly with the AutoMapper profiles of the database provider selected by Features:DatabaseMode
    public static Assembly GetMappingProfilesAssembly(IConfiguration configuration)
    {
        return configuration.GetValue<string>("Features:DatabaseMode") switch
        {
            "MongoDB" => typeof(MongoDBServicesRegistration).Assembly,
            "CosmosDB" => typeof(CosmosDBServicesRegistration).Assembly,
            _ => typeof(EntityFrameworkServiceRegistration).Assembly
        };
    }
}
