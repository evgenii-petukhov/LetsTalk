using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Persistence.MongoDB.Repository.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace LetsTalk.Server.Persistence.MongoDB.Repository;

public static class MongoDBRepositoryRegistration
{
    public static IServiceCollection AddMongoDBRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IMongoClient>(new MongoClient(configuration.GetConnectionString("MongoDB")));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<ILinkPreviewRepository, LinkPreviewRepository>();
        services.AddScoped<IChatMessageStatusRepository, ChatMessageStatusRepository>();
        services.Configure<MongoDBSettings>(configuration.GetSection("MongoDB"));

        return services;
    }
}
