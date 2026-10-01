using LetsTalk.Server.Persistence.CosmosDB.Models;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;

public interface IChatMessageStatusRepository
{
    Task MarkAsReadAsync(
        string chatId,
        string accountId,
        string messageId,
        CancellationToken cancellationToken = default);

    Task<List<ChatMessageStatus>> GetStatusesByAccountIdAndChatIdsAsync(
        string accountId,
        IReadOnlyList<string> chatIds,
        CancellationToken cancellationToken = default);
}
