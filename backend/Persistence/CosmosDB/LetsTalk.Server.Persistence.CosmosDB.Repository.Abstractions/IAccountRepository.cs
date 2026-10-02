using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.Enums;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;

public interface IAccountRepository
{
    Task<Account> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<Account> GetByEmailAsync(
        string email,
        AccountTypes accountType,
        CancellationToken cancellationToken = default);

    Task<Account> CreateAccountAsync(
        AccountTypes accountType,
        string email,
        CancellationToken cancellationToken = default);

    Task<Account> UpdateProfileAsync(
        string id,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task<Account> UpdateProfileAsync(
        string id,
        string firstName,
        string lastName,
        string imageId,
        int width,
        int height,
        ImageFormats imageFormat,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default);

    Task<List<Account>> GetAccountsByChatsAsync(
        IEnumerable<Chat> chats,
        string accountId,
        CancellationToken cancellationToken = default);

    Task<List<Account>> GetAccountsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> IsAccountIdValidAsync(
        string id, CancellationToken
        cancellationToken = default);
}
