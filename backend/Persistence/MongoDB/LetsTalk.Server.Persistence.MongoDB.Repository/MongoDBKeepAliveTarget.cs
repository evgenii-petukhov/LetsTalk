using LetsTalk.Server.KeepAlive;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LetsTalk.Server.Persistence.MongoDB.Repository;

// MongoDB Atlas pauses a free cluster after 30 days with zero connections. The ping admin command needs a
// connection and touches no collection, so nothing is written.
public class MongoDBKeepAliveTarget(IMongoClient mongoClient) : IKeepAliveTarget
{
    private const string AdminDatabaseName = "admin";
    private const string PingCommandName = "ping";

    private readonly IMongoClient _mongoClient = mongoClient;

    public string Name => "MongoDB";

    public Task KeepAliveAsync(CancellationToken cancellationToken) =>
        _mongoClient
            .GetDatabase(AdminDatabaseName)
            .RunCommandAsync(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument(PingCommandName, 1)),
                cancellationToken: cancellationToken);
}
