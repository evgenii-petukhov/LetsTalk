using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.DateHelpers;
using LetsTalk.Server.Persistence.MongoDB.Models;
using LetsTalk.Server.Persistence.MongoDB.Repository.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LetsTalk.Server.Persistence.MongoDB.Repository;

public class ChatMessageStatusRepository : IChatMessageStatusRepository
{
    private static readonly ReplaceOptions UpsertReplaceOptions = new()
    {
        IsUpsert = true
    };

    private readonly IMongoCollection<ChatMessageStatus> _chatMessageStatusCollection;

    public ChatMessageStatusRepository(
        IMongoClient mongoClient,
        IOptions<MongoDBSettings> mongoDBSettings)
    {
        var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);

        _chatMessageStatusCollection = mongoDatabase.GetCollection<ChatMessageStatus>(nameof(ChatMessageStatus));
    }

    public Task MarkAsReadAsync(string chatId, string accountId, string messageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ChatMessageStatus>.Filter.And(
            Builders<ChatMessageStatus>.Filter.Eq(x => x.ChatId, chatId),
            Builders<ChatMessageStatus>.Filter.Eq(x => x.AccountId, accountId),
            Builders<ChatMessageStatus>.Filter.Eq(x => x.MessageId, messageId)
        );

        var chatMessageStatus = new ChatMessageStatus
        {
            ChatId = chatId,
            AccountId = accountId,
            MessageId = messageId,
            DateReadUnix = DateHelper.GetUnixTimestamp()
        };

        return _chatMessageStatusCollection.ReplaceOneAsync(filter, chatMessageStatus, UpsertReplaceOptions, cancellationToken);
    }
}
