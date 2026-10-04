using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository;

public static class CosmosDBRepositoryRegistration
{
    public static async Task<IServiceCollection> AddCosmosDBRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string uri = configuration.GetConnectionString("CosmosDB")!;
        var cosmosSection = configuration.GetSection("CosmosDB");
        string primaryKey = cosmosSection["PrimaryKey"]!;

        var cosmosClient = new CosmosClient(uri, primaryKey, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct
        });

        Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            cosmosSection["DatabaseName"]!,
            throughput: 1000,
            cancellationToken: default);

        Container accountContainer = await database.CreateContainerIfNotExistsAsync(
            nameof(Account),
            "/id",
            cancellationToken: default);

        Container chatContainer = await database.CreateContainerIfNotExistsAsync(
            nameof(Chat),
            "/id",
            cancellationToken: default);

        Container messageContainer = await database.CreateContainerIfNotExistsAsync(
            nameof(Message),
            "/chatId",
            cancellationToken: default);

        Container chatMessageStatusContainer = await database.CreateContainerIfNotExistsAsync(
            nameof(ChatMessageStatus),
            "/chatId",
            cancellationToken: default);

        Container linkPreviewContainer = await database.CreateContainerIfNotExistsAsync(
            nameof(LinkPreview),
            "/url",
            cancellationToken: default);

        services.AddKeyedSingleton(nameof(Account), accountContainer);
        services.AddKeyedSingleton(nameof(Chat), chatContainer);
        services.AddKeyedSingleton(nameof(Message), messageContainer);
        services.AddKeyedSingleton(nameof(ChatMessageStatus), chatMessageStatusContainer);
        services.AddKeyedSingleton(nameof(LinkPreview), linkPreviewContainer);

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<ILinkPreviewRepository, LinkPreviewRepository>();
        services.AddScoped<IChatMessageStatusRepository, ChatMessageStatusRepository>();
        services.Configure<CosmosDBSettings>(configuration.GetSection("CosmosDB"));

        return services;
    }
}
