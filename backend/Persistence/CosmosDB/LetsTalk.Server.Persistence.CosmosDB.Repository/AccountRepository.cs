using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository;

public class AccountRepository(
    [FromKeyedServices(nameof(Account))] Container container) : IAccountRepository
{
    private readonly Container _container = container;

    public async Task<Account> CreateAccountAsync(AccountTypes accountType, string email, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            Id = Guid.CreateVersion7().ToString("N"),
            AccountTypeId = (int)accountType,
            Email = email
        };

        var response = await _container.CreateItemAsync(
            account,
            new PartitionKey(account.Id!),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<Account> GetByEmailAsync(string email, AccountTypes accountType, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 * " +
            "FROM c " +
            "WHERE c.email = @email AND c.accountTypeId = @accountTypeId")
            .WithParameter("@email", email)
            .WithParameter("@accountTypeId", (int)accountType);

        using var iterator = _container.GetItemQueryIterator<Account>(
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

    public async Task<Account> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<Account>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null!;
        }
    }

    public async Task<Account> UpdateProfileAsync(string id, string firstName, string lastName, CancellationToken cancellationToken = default)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set("/firstName", firstName),
            PatchOperation.Set("/lastName", lastName)
        };

        var response = await _container.PatchItemAsync<Account>(
            id,
            new PartitionKey(id),
            patchOperations,
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<Account> UpdateProfileAsync(
        string id,
        string firstName,
        string lastName,
        string imageId,
        int width,
        int height,
        ImageFormats imageFormat,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default)
    {
        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set("/firstName", firstName),
            PatchOperation.Set("/lastName", lastName)
        };

        if (!string.IsNullOrEmpty(imageId))
        {
            patchOperations.Add(PatchOperation.Set("/image", new Image
            {
                Id = imageId,
                Width = width,
                Height = height,
                ImageFormatId = (int)imageFormat,
                FileStorageTypeId = (int)fileStorageType
            }));
        }

        var response = await _container.PatchItemAsync<Account>(
            id,
            new PartitionKey(id),
            patchOperations,
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<List<Account>> GetAccountsByChatsAsync(IEnumerable<Chat> chats, string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var accountIds = chats
            .SelectMany(x => x.AccountIds ?? Enumerable.Empty<string>())
            .Where(x => !string.Equals(x, accountId, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (accountIds.Count == 0)
        {
            return [];
        }

        var items = accountIds
            .Select(id => (id, new PartitionKey(id)))
            .ToList();

        var response = await _container.ReadManyItemsAsync<Account>(
            items,
            cancellationToken: cancellationToken);

        return [.. response];
    }

    public async Task<List<Account>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c");

        using var iterator = _container.GetItemQueryIterator<Account>(query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 100
            });

        var accounts = new List<Account>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            accounts.AddRange(page);
        }

        return accounts;
    }

    public async Task<bool> IsAccountIdValidAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        try
        {
            var page = await _container.ReadItemAsync<Account>(id, new PartitionKey(id), cancellationToken: cancellationToken);

            return page.StatusCode == HttpStatusCode.OK;
        }
        catch (CosmosException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
