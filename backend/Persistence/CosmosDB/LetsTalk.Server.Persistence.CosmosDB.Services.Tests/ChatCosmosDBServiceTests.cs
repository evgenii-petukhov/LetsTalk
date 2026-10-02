using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.CosmosDB.Services;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class ChatCosmosDBServiceTests
{
    private Mock<IChatRepository> _chatRepository = null!;
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<IMessageRepository> _messageRepository = null!;
    private Mock<IChatMessageStatusRepository> _statusRepository = null!;
    private ChatCosmosDBService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _chatRepository = new Mock<IChatRepository>();
        _accountRepository = new Mock<IAccountRepository>();
        _messageRepository = new Mock<IMessageRepository>();
        _statusRepository = new Mock<IChatMessageStatusRepository>();
        _service = new ChatCosmosDBService(
            _chatRepository.Object,
            _accountRepository.Object,
            _messageRepository.Object,
            _statusRepository.Object);
    }

    [Test]
    public async Task CreateIndividualChatAsync_WhenChatExists_ShouldReturnExistingIdWithoutCreating()
    {
        var accountIds = new[] { "account-1", "account-2" };
        var cancellationToken = new CancellationToken(canceled: true);
        _chatRepository
            .Setup(x => x.GetIndividualChatByAccountIdsAsync(accountIds, cancellationToken))
            .ReturnsAsync(new Chat { Id = "chat-existing" });

        var result = await _service.CreateIndividualChatAsync(accountIds, cancellationToken);

        result.Should().Be("chat-existing");
        _chatRepository.Verify(x => x.GetIndividualChatByAccountIdsAsync(accountIds, cancellationToken), Times.Once);
        _chatRepository.Verify(x => x.CreateIndividualChatAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateIndividualChatAsync_WhenChatDoesNotExist_ShouldCreateAndReturnId()
    {
        var accountIds = new[] { "account-1", "account-2" };
        var cancellationToken = new CancellationToken(canceled: true);
        _chatRepository
            .Setup(x => x.GetIndividualChatByAccountIdsAsync(accountIds, cancellationToken))
            .ReturnsAsync((Chat)null!);
        _chatRepository
            .Setup(x => x.CreateIndividualChatAsync(accountIds, cancellationToken))
            .ReturnsAsync(new Chat { Id = "chat-created" });

        var result = await _service.CreateIndividualChatAsync(accountIds, cancellationToken);

        result.Should().Be("chat-created");
        _chatRepository.Verify(x => x.GetIndividualChatByAccountIdsAsync(accountIds, cancellationToken), Times.Once);
        _chatRepository.Verify(x => x.CreateIndividualChatAsync(accountIds, cancellationToken), Times.Once);
    }

    [Test]
    public async Task GetAccountIdsInIndividualChatsAsync_ShouldForwardArgumentsAndResult()
    {
        const string accountId = "account-1";
        var cancellationToken = new CancellationToken(canceled: true);
        var expected = new List<string> { "account-2" };
        _chatRepository.Setup(x => x.GetAccountIdsInIndividualChatsAsync(accountId, cancellationToken)).ReturnsAsync(expected);

        var result = await _service.GetAccountIdsInIndividualChatsAsync(accountId, cancellationToken);

        result.Should().BeSameAs(expected);
        _chatRepository.Verify(x => x.GetAccountIdsInIndividualChatsAsync(accountId, cancellationToken), Times.Once);
    }

    [Test]
    public async Task GetChatMemberAccountIdsAsync_ShouldForwardArgumentsAndResult()
    {
        const string chatId = "chat-1";
        var cancellationToken = new CancellationToken(canceled: true);
        var expected = new List<string> { "account-1", "account-2" };
        _chatRepository.Setup(x => x.GetChatMemberAccountIdsAsync(chatId, cancellationToken)).ReturnsAsync(expected);

        var result = await _service.GetChatMemberAccountIdsAsync(chatId, cancellationToken);

        result.Should().BeSameAs(expected);
        _chatRepository.Verify(x => x.GetChatMemberAccountIdsAsync(chatId, cancellationToken), Times.Once);
    }

    [Test]
    public async Task IsAccountChatMemberAsync_ShouldForwardArgumentsAndReturnRepositoryResult()
    {
        const string chatId = "chat-1";
        const string accountId = "account-1";
        var cancellationToken = new CancellationToken(canceled: true);
        _chatRepository.Setup(x => x.IsAccountChatMemberAsync(chatId, accountId, cancellationToken)).ReturnsAsync(true);

        var result = await _service.IsAccountChatMemberAsync(chatId, accountId, cancellationToken);

        result.Should().BeTrue();
        _chatRepository.Verify(x => x.IsAccountChatMemberAsync(chatId, accountId, cancellationToken), Times.Once);
    }

    [Test]
    public async Task IsChatIdValidAsync_ShouldForwardArgumentsAndReturnRepositoryResult()
    {
        const string chatId = "chat-1";
        var cancellationToken = new CancellationToken(canceled: true);
        _chatRepository.Setup(x => x.IsChatIdValidAsync(chatId, cancellationToken)).ReturnsAsync(false);

        var result = await _service.IsChatIdValidAsync(chatId, cancellationToken);

        result.Should().BeFalse();
        _chatRepository.Verify(x => x.IsChatIdValidAsync(chatId, cancellationToken), Times.Once);
    }

    [Test]
    public async Task GetChatsAsync_ShouldMapChatDetailsAndUnreadMetrics()
    {
        const string accountId = "viewer";
        var cancellationToken = new CancellationToken(canceled: true);
        var individualChat = new Chat
        {
            Id = "individual-chat",
            IsIndividual = true,
            Name = "Ignored chat name",
            AccountIds = [accountId, "other-account"]
        };
        var groupChat = new Chat
        {
            Id = "group-chat",
            IsIndividual = false,
            Name = "Project group",
            AccountIds = [accountId, "member-account"],
            Image = new Image { Id = "group-image", FileStorageTypeId = 1 }
        };
        var chats = new List<Chat> { individualChat, groupChat };
        var otherAccount = new Account
        {
            Id = "other-account",
            FirstName = "Ada",
            LastName = "Lovelace",
            PhotoUrl = "https://example.com/photo.jpg",
            AccountTypeId = 3,
            Image = new Image { Id = "account-image", FileStorageTypeId = 2 }
        };
        _chatRepository.Setup(x => x.GetChatsByAccountIdAsync(accountId, cancellationToken)).ReturnsAsync(chats);
        _accountRepository
            .Setup(x => x.GetAccountsByChatsAsync(chats, accountId, cancellationToken))
            .ReturnsAsync(new List<Account> { otherAccount });
        _messageRepository
            .Setup(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), cancellationToken))
            .ReturnsAsync(new List<Message>
            {
                new() { Id = "own-message", ChatId = individualChat.Id, SenderId = accountId, DateCreatedUnix = 100 },
                new() { Id = "read-message", ChatId = individualChat.Id, SenderId = "other-account", DateCreatedUnix = 120 },
                new() { Id = "unread-message", ChatId = individualChat.Id, SenderId = "other-account", DateCreatedUnix = 150 }
            });
        _statusRepository
            .Setup(x => x.GetStatusesByAccountIdAndChatIdsAsync(
                accountId,
                It.Is<IReadOnlyList<string>>(ids => ids.SequenceEqual(new[] { "individual-chat", "group-chat" })),
                cancellationToken))
            .ReturnsAsync(new List<ChatMessageStatus>
            {
                new() { MessageId = "read-message", DateReadUnix = 120 }
            });

        var result = await _service.GetChatsAsync(accountId, cancellationToken);

        result.Should().HaveCount(2);
        var individualResult = result.Single(x => x.Id == individualChat.Id);
        individualResult.ChatName.Should().Be("Ada Lovelace");
        individualResult.PhotoUrl.Should().Be(otherAccount.PhotoUrl);
        individualResult.AccountTypeId.Should().Be(otherAccount.AccountTypeId);
        individualResult.Image.Should().BeEquivalentTo(new ImageServiceModel { Id = "account-image", FileStorageTypeId = 2 });
        individualResult.AccountIds.Should().Equal("other-account");
        individualResult.LastMessageDate.Should().Be(150);
        individualResult.LastMessageId.Should().Be("unread-message");
        individualResult.UnreadCount.Should().Be(1);
        individualResult.IsIndividual.Should().BeTrue();

        var groupResult = result.Single(x => x.Id == groupChat.Id);
        groupResult.ChatName.Should().Be("Project group");
        groupResult.PhotoUrl.Should().BeNull();
        groupResult.AccountTypeId.Should().BeNull();
        groupResult.Image.Should().BeNull();
        groupResult.AccountIds.Should().Equal("member-account");
        groupResult.LastMessageDate.Should().BeNull();
        groupResult.LastMessageId.Should().BeNull();
        groupResult.UnreadCount.Should().Be(0);
        groupResult.IsIndividual.Should().BeFalse();

        _accountRepository.Verify(x => x.GetAccountsByChatsAsync(chats, accountId, cancellationToken), Times.Once);
        _messageRepository.Verify(x => x.GetMessagesByChatIdsAsync(It.IsAny<IReadOnlyList<string>>(), cancellationToken), Times.Once);
        _statusRepository.Verify(
            x => x.GetStatusesByAccountIdAndChatIdsAsync(accountId, It.IsAny<IReadOnlyList<string>>(), cancellationToken),
            Times.Once);
    }
}
