using AutoMapper;
using LetsTalk.Server.Persistence.AgnosticServices.Abstractions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;

namespace LetsTalk.Server.Persistence.CosmosDB.Services;

public class AccountCosmosDBService(
    IAccountRepository accountRepository,
    IMapper mapper) : IAccountAgnosticService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<List<AccountServiceModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetAccountsAsync(cancellationToken);

        return _mapper.Map<List<AccountServiceModel>>(accounts);
    }

    public async Task<string> GetOrCreateAsync(AccountTypes accountType, string email, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByEmailAsync(email, accountType, cancellationToken);

        if (account != null)
        {
            return account.Id!;
        }

        try
        {
            account = await _accountRepository.CreateAccountAsync(
                accountType,
                email,
                cancellationToken);

            return account.Id!;
        }
        catch
        {
            account = await _accountRepository.GetByEmailAsync(email, accountType, cancellationToken);
            return account?.Id!;
        }
    }

    public Task<bool> IsAccountIdValidAsync(string id, CancellationToken cancellationToken = default)
    {
        return _accountRepository.IsAccountIdValidAsync(id, cancellationToken);
    }
}
