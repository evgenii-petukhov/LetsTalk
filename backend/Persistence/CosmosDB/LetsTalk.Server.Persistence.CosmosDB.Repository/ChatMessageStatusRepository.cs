using LetsTalk.Server.DateHelpers;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository;

public class ChatMessageStatusRepository(
    [FromKeyedServices(nameof(ChatMessageStatus))] Container container) : IChatMessageStatusRepository
{
    private readonly Container _container = container;

    public async Task MarkAsReadAsync(
        string chatId,
        string accountId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var query = new QueryDefinition(
            "SELECT TOP 1 * " +
            "FROM c " +
            "WHERE c.chatId = @chatId AND c.accountId = @accountId AND c.messageId = @messageId")
            .WithParameter("@chatId", chatId)
            .WithParameter("@accountId", accountId)
            .WithParameter("@messageId", messageId);

        using var iterator = _container.GetItemQueryIterator<ChatMessageStatus>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 1
            });

        ChatMessageStatus? item = null;

        if (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);

            item = page.FirstOrDefault();
        }

        item = item ?? new ChatMessageStatus
        {
            Id = Guid.CreateVersion7().ToString("N"),
            ChatId = chatId,
            AccountId = accountId,
            MessageId = messageId,
            DateReadUnix = DateHelper.GetUnixTimestamp()
        };

        await _container.UpsertItemAsync(
            item,
            new PartitionKey(chatId),
            cancellationToken: cancellationToken);
    }

    public async Task<List<ChatMessageStatus>> GetStatusesByAccountIdAndChatIdsAsync(
        string accountId,
        IReadOnlyList<string> chatIds,
        CancellationToken cancellationToken = default)
    {
        var inClause = string.Join(",", chatIds.Select((_, i) => $"@C{i}"));
        var query = new QueryDefinition(
            "SELECT c.messageId, c.dateReadUnix " +
            "FROM c " +
            $"WHERE c.accountId = @accountId AND c.chatId IN ({inClause})")
            .WithParameter("@accountId", accountId);

        for (int i = 0; i < chatIds.Count; i++)
        {
            query = query.WithParameter($"@C{i}", chatIds[i]);
        }

        using var iterator = _container.GetItemQueryIterator<ChatMessageStatus>(query);
        var statuses = new List<ChatMessageStatus>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            statuses.AddRange(page);
        }
        return statuses;
    }
}
