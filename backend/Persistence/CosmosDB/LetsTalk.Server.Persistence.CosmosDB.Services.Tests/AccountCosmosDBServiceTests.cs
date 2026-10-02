using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using LetsTalk.Server.Persistence.CosmosDB.Services;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class AccountCosmosDBServiceTests
{
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<IMapper> _mapper = null!;
    private AccountCosmosDBService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepository = new Mock<IAccountRepository>();
        _mapper = new Mock<IMapper>();
        _service = new AccountCosmosDBService(_accountRepository.Object, _mapper.Object);
    }

    [Test]
    public async Task GetAccountsAsync_ShouldReturnMappedAccountsAndForwardCancellationToken()
    {
        var cancellationToken = new CancellationToken(canceled: true);
        var accounts = new List<Account> { new() { Id = "account-1" } };
        var expected = new List<AccountServiceModel> { new() { Id = "account-1" } };
        _accountRepository.Setup(x => x.GetAccountsAsync(cancellationToken)).ReturnsAsync(accounts);
        _mapper.Setup(x => x.Map<List<AccountServiceModel>>(accounts)).Returns(expected);

        var result = await _service.GetAccountsAsync(cancellationToken);

        result.Should().BeSameAs(expected);
        _accountRepository.Verify(x => x.GetAccountsAsync(cancellationToken), Times.Once);
        _mapper.Verify(x => x.Map<List<AccountServiceModel>>(accounts), Times.Once);
    }

    [Test]
    public async Task GetOrCreateAsync_WhenAccountExists_ShouldReturnItsIdWithoutCreating()
    {
        const string email = "person@example.com";
        const AccountTypes accountType = AccountTypes.Email;
        var cancellationToken = new CancellationToken(canceled: true);
        _accountRepository
            .Setup(x => x.GetByEmailAsync(email, accountType, cancellationToken))
            .ReturnsAsync(new Account { Id = "account-1" });

        var result = await _service.GetOrCreateAsync(accountType, email, cancellationToken);

        result.Should().Be("account-1");
        _accountRepository.Verify(x => x.CreateAccountAsync(It.IsAny<AccountTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetOrCreateAsync_WhenAccountDoesNotExist_ShouldCreateAndReturnItsId()
    {
        const string email = "person@example.com";
        const AccountTypes accountType = AccountTypes.Email;
        var cancellationToken = new CancellationToken(canceled: true);
        _accountRepository
            .SetupSequence(x => x.GetByEmailAsync(email, accountType, cancellationToken))
            .ReturnsAsync((Account)null!);
        _accountRepository
            .Setup(x => x.CreateAccountAsync(accountType, email, cancellationToken))
            .ReturnsAsync(new Account { Id = "created-account" });

        var result = await _service.GetOrCreateAsync(accountType, email, cancellationToken);

        result.Should().Be("created-account");
        _accountRepository.Verify(x => x.GetByEmailAsync(email, accountType, cancellationToken), Times.Once);
        _accountRepository.Verify(x => x.CreateAccountAsync(accountType, email, cancellationToken), Times.Once);
    }

    [Test]
    public async Task GetOrCreateAsync_WhenCreateFails_ShouldReturnIdFromFollowUpLookup()
    {
        const string email = "person@example.com";
        const AccountTypes accountType = AccountTypes.Email;
        var cancellationToken = new CancellationToken(canceled: true);
        _accountRepository
            .SetupSequence(x => x.GetByEmailAsync(email, accountType, cancellationToken))
            .ReturnsAsync((Account)null!)
            .ReturnsAsync(new Account { Id = "created-by-another-request" });
        _accountRepository
            .Setup(x => x.CreateAccountAsync(accountType, email, cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Account already exists"));

        var result = await _service.GetOrCreateAsync(accountType, email, cancellationToken);

        result.Should().Be("created-by-another-request");
        _accountRepository.Verify(x => x.GetByEmailAsync(email, accountType, cancellationToken), Times.Exactly(2));
        _accountRepository.Verify(x => x.CreateAccountAsync(accountType, email, cancellationToken), Times.Once);
    }

    [Test]
    public async Task IsAccountIdValidAsync_ShouldForwardArgumentsAndReturnRepositoryResult()
    {
        const string accountId = "account-1";
        var cancellationToken = new CancellationToken(canceled: true);
        _accountRepository.Setup(x => x.IsAccountIdValidAsync(accountId, cancellationToken)).ReturnsAsync(true);

        var result = await _service.IsAccountIdValidAsync(accountId, cancellationToken);

        result.Should().BeTrue();
        _accountRepository.Verify(x => x.IsAccountIdValidAsync(accountId, cancellationToken), Times.Once);
    }
}
