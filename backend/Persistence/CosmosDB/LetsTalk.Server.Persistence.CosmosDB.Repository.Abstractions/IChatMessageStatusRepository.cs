namespace LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;

public interface IChatMessageStatusRepository
{
    Task MarkAsReadAsync(string chatId, string accountId, string messageId, CancellationToken cancellationToken = default);
}
