using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class ChatCosmosDBServiceTests
{
    private const string AccountId = "acc-001";
    private const string OtherAccountId = "acc-002";
    private const string ChatId = "chat-001";
    private const string OtherChatId = "chat-002";
    private const string GroupName = "Project Team";
    private const string OtherFirstName = "Bob";
    private const string OtherLastName = "Pettit";
    private const string PhotoUrl = "https://example.com/bob.jpg";
    private const string ImageId = "img-001";
    private const int TestAccountType = (int)AccountTypes.Email;
    private const int TestStorageType = (int)FileStorageTypes.AmazonS3;

    private Mock<IChatRepository> _mockChatRepository;
    private Mock<IAccountRepository> _mockAccountRepository;
    private Mock<IMessageRepository> _mockMessageRepository;
    private Mock<IChatMessageStatusRepository> _mockChatMessageStatusRepository;
    private ChatCosmosDBService _service;

    [SetUp]
    public void SetUp()
    {
        _mockChatRepository = new Mock<IChatRepository>();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockMessageRepository = new Mock<IMessageRepository>();
        _mockChatMessageStatusRepository = new Mock<IChatMessageStatusRepository>();
        _service = new ChatCosmosDBService(
            _mockChatRepository.Object,
            _mockAccountRepository.Object,
            _mockMessageRepository.Object,
            _mockChatMessageStatusRepository.Object);
    }

    [TestFixture]
    public class CreateIndividualChatAsyncTests : ChatCosmosDBServiceTests
    {
        [Test]
        public async Task CreateIndividualChatAsync_WhenChatExists_ShouldReturnExistingChatId()
        {
            // Arrange
            var accountIds = new[] { AccountId, OtherAccountId };
            var existingChat = new Chat { Id = ChatId, IsIndividual = true, AccountIds = [.. accountIds] };

            _mockChatRepository
                .Setup(x => x.GetIndividualChatByAccountIdsAsync(accountIds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingChat);

            // Act
            var result = await _service.CreateIndividualChatAsync(accountIds);

            // Assert
            result.Should().Be(ChatId);
            _mockChatRepository.Verify(x => x.GetIndividualChatByAccountIdsAsync(accountIds, It.IsAny<CancellationToken>()), Times.Once);
            _mockChatRepository.Verify(x => x.CreateIndividualChatAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateIndividualChatAsync_WhenChatDoesNotExist_ShouldCreateAndReturnNewChatId()
        {
            // Arrange
            var accountIds = new[] { AccountId, OtherAccountId };
            var newChat = new Chat { Id = ChatId, IsIndividual = true, AccountIds = [.. accountIds] };

            _mockChatRepository
                .Setup(x => x.GetIndividualChatByAccountIdsAsync(accountIds, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Chat)null!);

            _mockChatRepository
                .Setup(x => x.CreateIndividualChatAsync(accountIds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newChat);

            // Act
            var result = await _service.CreateIndividualChatAsync(accountIds);

            // Assert
            result.Should().Be(ChatId);
            _mockChatRepository.Verify(x => x.GetIndividualChatByAccountIdsAsync(accountIds, It.IsAny<CancellationToken>()), Times.Once);
            _mockChatRepository.Verify(x => x.CreateIndividualChatAsync(accountIds, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateIndividualChatAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var accountIds = new[] { AccountId, OtherAccountId };
            var existingChat = new Chat { Id = ChatId };

            _mockChatRepository
                .Setup(x => x.GetIndividualChatByAccountIdsAsync(accountIds, cancellationToken))
                .ReturnsAsync(existingChat);

            // Act
            var result = await _service.CreateIndividualChatAsync(accountIds, cancellationToken);

            // Assert
            result.Should().Be(ChatId);
            _mockChatRepository.Verify(x => x.GetIndividualChatByAccountIdsAsync(accountIds, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class GetAccountIdsInIndividualChatsAsyncTests : ChatCosmosDBServiceTests
    {
        [Test]
        public async Task GetAccountIdsInIndividualChatsAsync_ShouldReturnAccountIdsFromRepository()
        {
            // Arrange
            var expectedIds = new List<string> { OtherAccountId };

            _mockChatRepository
                .Setup(x => x.GetAccountIdsInIndividualChatsAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedIds);

            // Act
            var result = await _service.GetAccountIdsInIndividualChatsAsync(AccountId);

            // Assert
            result.Should().BeEquivalentTo(expectedIds);
            _mockChatRepository.Verify(x => x.GetAccountIdsInIndividualChatsAsync(AccountId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetAccountIdsInIndividualChatsAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockChatRepository
                .Setup(x => x.GetAccountIdsInIndividualChatsAsync(AccountId, cancellationToken))
                .ReturnsAsync([]);

            // Act
            await _service.GetAccountIdsInIndividualChatsAsync(AccountId, cancellationToken);

            // Assert
            _mockChatRepository.Verify(x => x.GetAccountIdsInIndividualChatsAsync(AccountId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class GetChatMemberAccountIdsAsyncTests : ChatCosmosDBServiceTests
    {
        [Test]
        public async Task GetChatMemberAccountIdsAsync_ShouldReturnMemberIdsFromRepository()
        {
            // Arrange
            var expectedIds = new List<string> { AccountId, OtherAccountId };

            _mockChatRepository
                .Setup(x => x.GetChatMemberAccountIdsAsync(ChatId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedIds);

            // Act
            var result = await _service.GetChatMemberAccountIdsAsync(ChatId);

            // Assert
            result.Should().BeEquivalentTo(expectedIds);
            _mockChatRepository.Verify(x => x.GetChatMemberAccountIdsAsync(ChatId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetChatMemberAccountIdsAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockChatRepository
                .Setup(x => x.GetChatMemberAccountIdsAsync(ChatId, cancellationToken))
                .ReturnsAsync([]);

            // Act
            await _service.GetChatMemberAccountIdsAsync(ChatId, cancellationToken);

            // Assert
            _mockChatRepository.Verify(x => x.GetChatMemberAccountIdsAsync(ChatId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class IsAccountChatMemberAsyncTests : ChatCosmosDBServiceTests
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task IsAccountChatMemberAsync_ShouldReturnRepositoryResult(bool expectedResult)
        {
            // Arrange
            _mockChatRepository
                .Setup(x => x.IsAccountChatMemberAsync(ChatId, AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.IsAccountChatMemberAsync(ChatId, AccountId);

            // Assert
            result.Should().Be(expectedResult);
            _mockChatRepository.Verify(x => x.IsAccountChatMemberAsync(ChatId, AccountId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task IsAccountChatMemberAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockChatRepository
                .Setup(x => x.IsAccountChatMemberAsync(ChatId, AccountId, cancellationToken))
                .ReturnsAsync(true);

            // Act
            await _service.IsAccountChatMemberAsync(ChatId, AccountId, cancellationToken);

            // Assert
            _mockChatRepository.Verify(x => x.IsAccountChatMemberAsync(ChatId, AccountId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class IsChatIdValidAsyncTests : ChatCosmosDBServiceTests
    {
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task IsChatIdValidAsync_ShouldReturnRepositoryResult(bool expectedResult)
        {
            // Arrange
            _mockChatRepository
                .Setup(x => x.IsChatIdValidAsync(ChatId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.IsChatIdValidAsync(ChatId);

            // Assert
            result.Should().Be(expectedResult);
            _mockChatRepository.Verify(x => x.IsChatIdValidAsync(ChatId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task IsChatIdValidAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockChatRepository
                .Setup(x => x.IsChatIdValidAsync(ChatId, cancellationToken))
                .ReturnsAsync(true);

            // Act
            await _service.IsChatIdValidAsync(ChatId, cancellationToken);

            // Assert
            _mockChatRepository.Verify(x => x.IsChatIdValidAsync(ChatId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class GetChatsAsyncTests : ChatCosmosDBServiceTests
    {
        [Test]
        public async Task GetChatsAsync_WhenNoChatsExist_ShouldReturnEmptyList()
        {
            // Arrange
            _mockChatRepository
                .Setup(x => x.GetChatsByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockAccountRepository
                .Setup(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockMessageRepository
                .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockChatMessageStatusRepository
                .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _service.GetChatsAsync(AccountId);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetChatsAsync_WithIndividualChat_ShouldPopulateOtherAccountDetailsAndExcludeOwnId()
        {
            // Arrange
            var chat = new Chat
            {
                Id = ChatId,
                IsIndividual = true,
                AccountIds = [AccountId, OtherAccountId]
            };

            var otherAccount = new Account
            {
                Id = OtherAccountId,
                FirstName = OtherFirstName,
                LastName = OtherLastName,
                PhotoUrl = PhotoUrl,
                AccountTypeId = TestAccountType,
                Image = new Image
                {
                    Id = ImageId,
                    FileStorageTypeId = TestStorageType
                }
            };

            _mockChatRepository
                .Setup(x => x.GetChatsByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([chat]);

            _mockAccountRepository
                .Setup(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([otherAccount]);

            _mockMessageRepository
                .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockChatMessageStatusRepository
                .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _service.GetChatsAsync(AccountId);

            // Assert
            result.Should().HaveCount(1);
            var item = result[0];
            item.Id.Should().Be(ChatId);
            item.ChatName.Should().Be($"{OtherFirstName} {OtherLastName}");
            item.PhotoUrl.Should().Be(PhotoUrl);
            item.AccountTypeId.Should().Be(TestAccountType);
            item.Image.Should().NotBeNull();
            item.Image!.Id.Should().Be(ImageId);
            item.Image.FileStorageTypeId.Should().Be(TestStorageType);
            item.IsIndividual.Should().BeTrue();
            item.AccountIds.Should().BeEquivalentTo([OtherAccountId]);
            item.UnreadCount.Should().Be(0);
            item.LastMessageDate.Should().BeNull();
            item.LastMessageId.Should().BeNull();
        }

        [Test]
        public async Task GetChatsAsync_WithGroupChat_ShouldUseChatNameAndNullAccountSpecificFields()
        {
            // Arrange
            var chat = new Chat
            {
                Id = ChatId,
                Name = GroupName,
                IsIndividual = false,
                AccountIds = [AccountId, OtherAccountId]
            };

            _mockChatRepository
                .Setup(x => x.GetChatsByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([chat]);

            _mockAccountRepository
                .Setup(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockMessageRepository
                .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockChatMessageStatusRepository
                .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _service.GetChatsAsync(AccountId);

            // Assert
            result.Should().HaveCount(1);
            var item = result[0];
            item.ChatName.Should().Be(GroupName);
            item.PhotoUrl.Should().BeNull();
            item.AccountTypeId.Should().BeNull();
            item.Image.Should().BeNull();
            item.IsIndividual.Should().BeFalse();
            item.AccountIds.Should().BeEquivalentTo([OtherAccountId]);
        }

        [Test]
        public async Task GetChatsAsync_WithMessages_ShouldComputeCorrectMetricsAndUnreadCount()
        {
            // Arrange
            var chat = new Chat
            {
                Id = ChatId,
                IsIndividual = true,
                AccountIds = [AccountId, OtherAccountId]
            };

            var otherAccount = new Account
            {
                Id = OtherAccountId,
                FirstName = OtherFirstName,
                LastName = OtherLastName
            };

            var messages = new List<Message>
            {
                new() { Id = "msg-1", ChatId = ChatId, SenderId = OtherAccountId, DateCreatedUnix = 100 },
                new() { Id = "msg-2", ChatId = ChatId, SenderId = OtherAccountId, DateCreatedUnix = 200 },
                new() { Id = "msg-3", ChatId = ChatId, SenderId = AccountId, DateCreatedUnix = 300 }
            };

            // Account read up to msg-1 (timestamp 100)
            var statuses = new List<ChatMessageStatus>
            {
                new() { ChatId = ChatId, AccountId = AccountId, MessageId = "msg-1", DateReadUnix = 100 }
            };

            _mockChatRepository
                .Setup(x => x.GetChatsByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([chat]);

            _mockAccountRepository
                .Setup(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([otherAccount]);

            _mockMessageRepository
                .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);

            _mockChatMessageStatusRepository
                .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(statuses);

            // Act
            var result = await _service.GetChatsAsync(AccountId);

            // Assert
            result.Should().HaveCount(1);
            var item = result[0];
            item.LastMessageDate.Should().Be(300);
            item.LastMessageId.Should().Be("msg-3");
            // Only msg-2 is unread: msg-1 was read (date <= 100), msg-3 is sent by AccountId itself
            item.UnreadCount.Should().Be(1);
        }

        [Test]
        public async Task GetChatsAsync_WhenAllMessagesSentByCurrentUser_ShouldHaveZeroUnreadCount()
        {
            // Arrange
            var chat = new Chat
            {
                Id = ChatId,
                IsIndividual = true,
                AccountIds = [AccountId, OtherAccountId]
            };

            var messages = new List<Message>
            {
                new() { Id = "msg-1", ChatId = ChatId, SenderId = AccountId, DateCreatedUnix = 100 },
                new() { Id = "msg-2", ChatId = ChatId, SenderId = AccountId, DateCreatedUnix = 200 }
            };

            _mockChatRepository
                .Setup(x => x.GetChatsByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([chat]);

            _mockAccountRepository
                .Setup(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockMessageRepository
                .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);

            _mockChatMessageStatusRepository
                .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _service.GetChatsAsync(AccountId);

            // Assert
            result.Should().HaveCount(1);
            result[0].UnreadCount.Should().Be(0);
            result[0].LastMessageId.Should().Be("msg-2");
            result[0].LastMessageDate.Should().Be(200);
        }

        [Test]
        public async Task GetChatsAsync_WithMultipleChats_ShouldComputeMetricsIndependentlyForEachChat()
        {
            // Arrange
            var chat1 = new Chat { Id = ChatId, IsIndividual = true, AccountIds = [AccountId, OtherAccountId] };
            var chat2 = new Chat { Id = OtherChatId, Name = GroupName, IsIndividual = false, AccountIds = [AccountId, OtherAccountId] };

            var messages = new List<Message>
            {
                new() { Id = "msg-1", ChatId = ChatId, SenderId = OtherAccountId, DateCreatedUnix = 100 },
                new() { Id = "msg-2", ChatId = OtherChatId, SenderId = OtherAccountId, DateCreatedUnix = 500 }
            };

            var statuses = new List<ChatMessageStatus>
            {
                new() { ChatId = ChatId, AccountId = AccountId, MessageId = "msg-1", DateReadUnix = 50 },
                new() { ChatId = OtherChatId, AccountId = AccountId, MessageId = "msg-2", DateReadUnix = 500 }
            };

            _mockChatRepository
                .Setup(x => x.GetChatsByAccountIdAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([chat1, chat2]);

            _mockAccountRepository
                .Setup(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _mockMessageRepository
                .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);

            _mockChatMessageStatusRepository
                .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(statuses);

            // Act
            var result = await _service.GetChatsAsync(AccountId);

            // Assert
            result.Should().HaveCount(2);
            var item1 = result.First(c => c.Id == ChatId);
            var item2 = result.First(c => c.Id == OtherChatId);

            item1.LastMessageId.Should().Be("msg-1");
            item1.LastMessageDate.Should().Be(100);
            item1.UnreadCount.Should().Be(1);

            item2.LastMessageId.Should().Be("msg-2");
            item2.LastMessageDate.Should().Be(500);
            item2.UnreadCount.Should().Be(0);
        }

        [Test]
        public async Task GetChatsAsync_WithCancellationToken_ShouldPassTokenToAllRepositories()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockChatRepository
                .Setup(x => x.GetChatsByAccountIdAsync(AccountId, cancellationToken))
                .ReturnsAsync([]);

            _mockAccountRepository
                .Setup(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, cancellationToken))
                .ReturnsAsync([]);

            _mockMessageRepository
                .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), cancellationToken))
                .ReturnsAsync([]);

            _mockChatMessageStatusRepository
                .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), cancellationToken))
                .ReturnsAsync([]);

            // Act
            await _service.GetChatsAsync(AccountId, cancellationToken);

            // Assert
            _mockChatRepository.Verify(x => x.GetChatsByAccountIdAsync(AccountId, cancellationToken), Times.AtLeastOnce);
            _mockAccountRepository.Verify(x => x.GetAccountsByChatsAsync(It.IsAny<IEnumerable<Chat>>(), AccountId, cancellationToken), Times.Once);
            _mockMessageRepository.Verify(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), cancellationToken), Times.Once);
            _mockChatMessageStatusRepository.Verify(x => x.GetStatusesByAccountIdAndChatIdsAsync(AccountId, It.IsAny<IReadOnlyList<string>>(), cancellationToken), Times.Once);
        }
    }
}
