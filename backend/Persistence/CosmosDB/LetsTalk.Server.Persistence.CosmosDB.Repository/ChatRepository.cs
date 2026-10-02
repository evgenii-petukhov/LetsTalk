using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository;

public class ChatRepository(
    [FromKeyedServices(nameof(Chat))] Container container) : IChatRepository
{
    private readonly Container _container = container;

    public async Task<List<Chat>> GetChatsByAccountIdAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var query = new QueryDefinition(
            "SELECT * " +
            "FROM c " +
            "WHERE ARRAY_CONTAINS(c.accountIds, @accountId)")
            .WithParameter("@accountId", accountId);

        using var iterator = _container.GetItemQueryIterator<Chat>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 100
            });

        var chats = new List<Chat>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            chats.AddRange(page);
        }

        return chats;
    }

    public async Task<Chat> GetIndividualChatByAccountIdsAsync(
        IEnumerable<string> accountIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountIds);

        var ids = accountIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
        {
            return null!;
        }

        var whereClause = string.Join(
            " AND ",
            ids.Select((_, index) => $"ARRAY_CONTAINS(c.accountIds, @A{index})"));

        var query = new QueryDefinition(
            "SELECT TOP 1 * " +
            "FROM c " +
            $"WHERE c.isIndividual = true AND ARRAY_LENGTH(c.accountIds) = @accountCount AND {whereClause}")
            .WithParameter("@accountCount", ids.Length);

        for (int index = 0; index < ids.Length; index++)
        {
            query = query.WithParameter($"@A{index}", ids[index]);
        }

        using var iterator = _container.GetItemQueryIterator<Chat>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 1
            });


        if (!iterator.HasMoreResults)
        {
            return null!;
        }

        var page = await iterator.ReadNextAsync(cancellationToken);

        return page.FirstOrDefault()!;
    }

    public async Task<Chat> CreateIndividualChatAsync(
        IEnumerable<string> accountIds,
        CancellationToken cancellationToken = default)
    {
        var chat = new Chat
        {
            Id = Guid.CreateVersion7().ToString("N"),
            IsIndividual = true,
            AccountIds = [.. accountIds]
        };

        var response = await _container.CreateItemAsync(
            chat,
            new PartitionKey(chat.Id!),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<bool> IsChatIdValidAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidObjectId(id))
        {
            return false;
        }

        try
        {
            var response = await _container.ReadItemAsync<Chat>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (CosmosException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<List<string>> GetChatMemberAccountIdsAsync(
        string chatId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<Chat>(
                chatId,
                new PartitionKey(chatId),
                cancellationToken: cancellationToken);

            return response.Resource.AccountIds!;
        }
        catch (CosmosException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null!;
        }
    }

    public async Task<bool> IsAccountChatMemberAsync(
        string chatId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var query = new QueryDefinition(
            "SELECT * " +
            "FROM c " +
            "WHERE c.id = @chatId AND ARRAY_CONTAINS(c.accountIds, @accountId)")
            .WithParameter("@chatId", chatId)
            .WithParameter("@accountId", accountId);

        using var iterator = _container.GetItemQueryIterator<Chat>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 1
            });

        return iterator.HasMoreResults;
    }

    public async Task<List<string>> GetAccountIdsInIndividualChatsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var query = new QueryDefinition(
            "SELECT id " +
            "FROM c " +
            "WHERE c.isIndividual = true AND ARRAY_CONTAINS(c.accountIds, @accountId)")
            .WithParameter("@accountId", accountId);

        using var iterator = _container.GetItemQueryIterator<Chat>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 100
            });

        var accountIds = new List<string>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);

            var accountIdsToAdd = page
                .Select(p => p.Id!)
                .Where(id => !string.Equals(id, accountId, StringComparison.OrdinalIgnoreCase));

            accountIds.AddRange(accountIdsToAdd);
        }

        return accountIds;
    }

    private static bool IsValidObjectId(string? id)
    {
        return !string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out _);
    }
}
