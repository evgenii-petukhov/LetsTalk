using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class AccountCosmosDBServiceTests
{
    private const string AccountId = "acc-001";
    private const string OtherAccountId = "acc-002";
    private const string Email = "test@example.com";
    private const AccountTypes AccountType = AccountTypes.Email;

    private Mock<IAccountRepository> _mockAccountRepository;
    private Mock<IMapper> _mockMapper;
    private AccountCosmosDBService _service;

    [SetUp]
    public void SetUp()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockMapper = new Mock<IMapper>();
        _service = new AccountCosmosDBService(_mockAccountRepository.Object, _mockMapper.Object);
    }

    [TestFixture]
    public class GetOrCreateAsyncTests : AccountCosmosDBServiceTests
    {
        [Test]
        public async Task GetOrCreateAsync_WhenAccountExists_ShouldReturnExistingAccountId()
        {
            // Arrange
            var existingAccount = new Account
            {
                Id = AccountId,
                Email = Email,
                AccountTypeId = (int)AccountType
            };

            _mockAccountRepository
                .Setup(x => x.GetByEmailAsync(Email, AccountType, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingAccount);

            // Act
            var result = await _service.GetOrCreateAsync(AccountType, Email);

            // Assert
            result.Should().Be(AccountId);
            _mockAccountRepository.Verify(x => x.GetByEmailAsync(Email, AccountType, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.CreateAccountAsync(It.IsAny<AccountTypes>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GetOrCreateAsync_WhenAccountDoesNotExist_ShouldCreateNewAccountAndReturnId()
        {
            // Arrange
            var newAccount = new Account
            {
                Id = AccountId,
                Email = Email,
                AccountTypeId = (int)AccountType
            };

            _mockAccountRepository
                .Setup(x => x.GetByEmailAsync(Email, AccountType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Account)null!);

            _mockAccountRepository
                .Setup(x => x.CreateAccountAsync(AccountType, Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newAccount);

            // Act
            var result = await _service.GetOrCreateAsync(AccountType, Email);

            // Assert
            result.Should().Be(AccountId);
            _mockAccountRepository.Verify(x => x.GetByEmailAsync(Email, AccountType, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.CreateAccountAsync(AccountType, Email, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetOrCreateAsync_WhenCreateThrowsException_ShouldRetryGetByEmailAndReturnId()
        {
            // Arrange
            var existingAccount = new Account
            {
                Id = AccountId,
                Email = Email,
                AccountTypeId = (int)AccountType
            };

            _mockAccountRepository
                .SetupSequence(x => x.GetByEmailAsync(Email, AccountType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Account)null!)
                .ReturnsAsync(existingAccount);

            _mockAccountRepository
                .Setup(x => x.CreateAccountAsync(AccountType, Email, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Account already exists"));

            // Act
            var result = await _service.GetOrCreateAsync(AccountType, Email);

            // Assert
            result.Should().Be(AccountId);
            _mockAccountRepository.Verify(x => x.GetByEmailAsync(Email, AccountType, It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockAccountRepository.Verify(x => x.CreateAccountAsync(AccountType, Email, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetOrCreateAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var account = new Account { Id = AccountId };

            _mockAccountRepository
                .Setup(x => x.GetByEmailAsync(Email, AccountType, cancellationToken))
                .ReturnsAsync(account);

            // Act
            var result = await _service.GetOrCreateAsync(AccountType, Email, cancellationToken);

            // Assert
            result.Should().Be(AccountId);
            _mockAccountRepository.Verify(x => x.GetByEmailAsync(Email, AccountType, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class GetAccountsAsyncTests : AccountCosmosDBServiceTests
    {
        [Test]
        public async Task GetAccountsAsync_WithAccounts_ShouldReturnMappedAccountList()
        {
            // Arrange
            var accounts = new List<Account>
            {
                new() { Id = AccountId, FirstName = "John", LastName = "Doe", AccountTypeId = (int)AccountTypes.Email },
                new() { Id = OtherAccountId, FirstName = "Jane", LastName = "Smith", AccountTypeId = (int)AccountTypes.Facebook }
            };

            var expectedModels = new List<AccountServiceModel>
            {
                new() { Id = AccountId, FirstName = "John", LastName = "Doe", AccountTypeId = (int)AccountTypes.Email },
                new() { Id = OtherAccountId, FirstName = "Jane", LastName = "Smith", AccountTypeId = (int)AccountTypes.Facebook }
            };

            _mockAccountRepository
                .Setup(x => x.GetAccountsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(accounts);

            _mockMapper
                .Setup(x => x.Map<List<AccountServiceModel>>(accounts))
                .Returns(expectedModels);

            // Act
            var result = await _service.GetAccountsAsync();

            // Assert
            result.Should().BeEquivalentTo(expectedModels);
            _mockAccountRepository.Verify(x => x.GetAccountsAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(x => x.Map<List<AccountServiceModel>>(accounts), Times.Once);
        }

        [Test]
        public async Task GetAccountsAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var accounts = new List<Account>();

            _mockAccountRepository
                .Setup(x => x.GetAccountsAsync(cancellationToken))
                .ReturnsAsync(accounts);

            _mockMapper
                .Setup(x => x.Map<List<AccountServiceModel>>(accounts))
                .Returns([]);

            // Act
            await _service.GetAccountsAsync(cancellationToken);

            // Assert
            _mockAccountRepository.Verify(x => x.GetAccountsAsync(cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class IsAccountIdValidAsyncTests : AccountCosmosDBServiceTests
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task IsAccountIdValidAsync_ShouldReturnRepositoryResult(bool expectedResult)
        {
            // Arrange
            _mockAccountRepository
                .Setup(x => x.IsAccountIdValidAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.IsAccountIdValidAsync(AccountId);

            // Assert
            result.Should().Be(expectedResult);
            _mockAccountRepository.Verify(x => x.IsAccountIdValidAsync(AccountId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task IsAccountIdValidAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockAccountRepository
                .Setup(x => x.IsAccountIdValidAsync(AccountId, cancellationToken))
                .ReturnsAsync(true);

            // Act
            await _service.IsAccountIdValidAsync(AccountId, cancellationToken);

            // Assert
            _mockAccountRepository.Verify(x => x.IsAccountIdValidAsync(AccountId, cancellationToken), Times.Once);
        }
    }
}
