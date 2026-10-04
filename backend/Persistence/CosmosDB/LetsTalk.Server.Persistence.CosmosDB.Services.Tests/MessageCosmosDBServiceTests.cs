using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class MessageCosmosDBServiceTests
{
    private const string SenderId = "sender-001";
    private const string ChatId = "chat-001";
    private const string MessageId = "msg-001";
    private const string LinkPreviewId = "preview-001";
    private const string MessageText = "Hello world";
    private const string MessageHtml = "<p>Hello world</p>";
    private const string ImageId = "img-001";
    private const string Filename = "image.png";
    private const string Url = "https://example.com";
    private const string Title = "Example Title";
    private const string ImageUrl = "https://example.com/image.png";
    private const int ImageWidth = 800;
    private const int ImageHeight = 600;
    private const int PageIndex = 0;
    private const int PageSize = 10;
    private const ImageFormats TestImageFormat = ImageFormats.Jpeg;
    private const FileStorageTypes TestStorageType = FileStorageTypes.AmazonS3;

    private Mock<IMessageRepository> _mockMessageRepository;
    private Mock<IChatMessageStatusRepository> _mockChatMessageStatusRepository;
    private Mock<ILinkPreviewRepository> _mockLinkPreviewRepository;
    private Mock<IMapper> _mockMapper;
    private MessageCosmosDBService _service;

    [SetUp]
    public void SetUp()
    {
        _mockMessageRepository = new Mock<IMessageRepository>();
        _mockChatMessageStatusRepository = new Mock<IChatMessageStatusRepository>();
        _mockLinkPreviewRepository = new Mock<ILinkPreviewRepository>();
        _mockMapper = new Mock<IMapper>();
        _service = new MessageCosmosDBService(
            _mockMessageRepository.Object,
            _mockChatMessageStatusRepository.Object,
            _mockLinkPreviewRepository.Object,
            _mockMapper.Object);
    }

    [TestFixture]
    public class CreateMessageAsyncWithLinkPreviewTests : MessageCosmosDBServiceTests
    {
        [Test]
        public async Task CreateMessageAsync_WithLinkPreview_ShouldCreateMessageFetchPreviewAndReturnMappedResult()
        {
            // Arrange
            var createdMessage = new Message
            {
                Id = MessageId,
                SenderId = SenderId,
                ChatId = ChatId,
                Text = MessageText,
                TextHtml = MessageHtml,
                LinkPreviewId = LinkPreviewId
            };

            var linkPreview = new LinkPreview
            {
                Id = LinkPreviewId,
                Url = Url,
                Title = Title
            };

            var expectedModel = new MessageServiceModel
            {
                Id = MessageId,
                SenderId = SenderId,
                ChatId = ChatId,
                Text = MessageText,
                TextHtml = MessageHtml
            };

            var expectedLinkPreviewModel = new LinkPreviewServiceModel
            {
                Url = Url,
                Title = Title
            };

            _mockMessageRepository
                .Setup(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, false, 0, LinkPreviewId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdMessage);

            _mockLinkPreviewRepository
                .Setup(x => x.GetByIdAsync(LinkPreviewId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(linkPreview);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(createdMessage))
                .Returns(expectedModel);

            _mockMapper
                .Setup(x => x.Map<LinkPreviewServiceModel>(linkPreview))
                .Returns(expectedLinkPreviewModel);

            // Act
            var result = await _service.CreateMessageAsync(SenderId, ChatId, MessageText, MessageHtml, false, 0, LinkPreviewId, default);

            // Assert
            result.Should().BeEquivalentTo(expectedModel);
            result.LinkPreview.Should().BeEquivalentTo(expectedLinkPreviewModel);
            _mockMessageRepository.Verify(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, false, 0, LinkPreviewId, It.IsAny<CancellationToken>()), Times.Once);
            _mockLinkPreviewRepository.Verify(x => x.GetByIdAsync(LinkPreviewId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateMessageAsync_WithLinkPreview_WithCancellationToken_ShouldPassTokenToRepositories()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var createdMessage = new Message { Id = MessageId, LinkPreviewId = LinkPreviewId };
            var linkPreview = new LinkPreview { Id = LinkPreviewId };

            _mockMessageRepository
                .Setup(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, false, 0, LinkPreviewId, cancellationToken))
                .ReturnsAsync(createdMessage);

            _mockLinkPreviewRepository
                .Setup(x => x.GetByIdAsync(LinkPreviewId, cancellationToken))
                .ReturnsAsync(linkPreview);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(createdMessage))
                .Returns(new MessageServiceModel { Id = MessageId });

            // Act
            await _service.CreateMessageAsync(SenderId, ChatId, MessageText, MessageHtml, false, 0, LinkPreviewId, cancellationToken);

            // Assert
            _mockMessageRepository.Verify(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, false, 0, LinkPreviewId, cancellationToken), Times.Once);
            _mockLinkPreviewRepository.Verify(x => x.GetByIdAsync(LinkPreviewId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class CreateMessageAsyncWithImageTests : MessageCosmosDBServiceTests
    {
        [Test]
        public async Task CreateMessageAsync_WithImage_ShouldCreateAndReturnMappedResult()
        {
            // Arrange
            var createdMessage = new Message
            {
                Id = MessageId,
                SenderId = SenderId,
                ChatId = ChatId,
                Text = MessageText,
                TextHtml = MessageHtml,
                Image = new Image
                {
                    Id = ImageId,
                    Width = ImageWidth,
                    Height = ImageHeight,
                    ImageFormatId = (int)TestImageFormat,
                    FileStorageTypeId = (int)TestStorageType
                }
            };

            var expectedModel = new MessageServiceModel
            {
                Id = MessageId,
                SenderId = SenderId,
                ChatId = ChatId,
                Text = MessageText,
                TextHtml = MessageHtml,
                Image = new ImageServiceModel
                {
                    Id = ImageId,
                    FileStorageTypeId = (int)TestStorageType
                }
            };

            _mockMessageRepository
                .Setup(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, ImageId, ImageWidth, ImageHeight, TestImageFormat, TestStorageType, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdMessage);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(createdMessage))
                .Returns(expectedModel);

            // Act
            var result = await _service.CreateMessageAsync(SenderId, ChatId, MessageText, MessageHtml, ImageId, ImageWidth, ImageHeight, TestImageFormat, TestStorageType, default);

            // Assert
            result.Should().BeEquivalentTo(expectedModel);
            _mockMessageRepository.Verify(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, ImageId, ImageWidth, ImageHeight, TestImageFormat, TestStorageType, It.IsAny<CancellationToken>()), Times.Once);
            _mockLinkPreviewRepository.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateMessageAsync_WithImage_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var createdMessage = new Message { Id = MessageId };

            _mockMessageRepository
                .Setup(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, ImageId, ImageWidth, ImageHeight, TestImageFormat, TestStorageType, cancellationToken))
                .ReturnsAsync(createdMessage);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(createdMessage))
                .Returns(new MessageServiceModel { Id = MessageId });

            // Act
            await _service.CreateMessageAsync(SenderId, ChatId, MessageText, MessageHtml, ImageId, ImageWidth, ImageHeight, TestImageFormat, TestStorageType, cancellationToken);

            // Assert
            _mockMessageRepository.Verify(x => x.CreateAsync(SenderId, ChatId, MessageText, MessageHtml, ImageId, ImageWidth, ImageHeight, TestImageFormat, TestStorageType, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class GetPagedAsyncTests : MessageCosmosDBServiceTests
    {
        [Test]
        public async Task GetPagedAsync_WhenMessagesHaveLinkPreviews_ShouldFetchBatchAndEnrichMessagesOrdered()
        {
            // Arrange
            var message1 = new Message { Id = "msg-1", DateCreatedUnix = 200, LinkPreviewId = LinkPreviewId };
            var message2 = new Message { Id = "msg-2", DateCreatedUnix = 100, LinkPreviewId = null };
            var messages = new List<Message> { message1, message2 };

            var linkPreview = new LinkPreview { Id = LinkPreviewId, Url = Url };
            var linkPreviewsDict = new Dictionary<string, LinkPreview> { { LinkPreviewId, linkPreview } };

            _mockMessageRepository
                .Setup(x => x.GetPagedAsync(ChatId, PageIndex, PageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);

            _mockLinkPreviewRepository
                .Setup(x => x.GetLinkPreviewsByIdAsync(It.Is<IEnumerable<string>>(ids => ids.Contains(LinkPreviewId)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(linkPreviewsDict);

            _mockMapper
                .Setup(x => x.Map<List<MessageServiceModel>>(It.Is<IEnumerable<Message>>(m => m.First().Id == "msg-2")))
                .Returns(
                [
                    new MessageServiceModel { Id = "msg-2", DateCreatedUnix = 100 },
                    new MessageServiceModel { Id = "msg-1", DateCreatedUnix = 200 }
                ]);

            // Act
            var result = await _service.GetPagedAsync(ChatId, PageIndex, PageSize);

            // Assert
            result.Should().HaveCount(2);
            message1.LinkPreview.Should().Be(linkPreview);
            _mockLinkPreviewRepository.Verify(x => x.GetLinkPreviewsByIdAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(x => x.Map<List<MessageServiceModel>>(It.Is<IEnumerable<Message>>(seq => seq.First().Id == "msg-2" && seq.Last().Id == "msg-1")), Times.Once);
        }

        [Test]
        public async Task GetPagedAsync_WhenNoMessagesHaveLinkPreviewId_ShouldNotCallLinkPreviewRepository()
        {
            // Arrange
            var messages = new List<Message>
            {
                new() { Id = "msg-1", DateCreatedUnix = 100 },
                new() { Id = "msg-2", DateCreatedUnix = 200 }
            };

            _mockMessageRepository
                .Setup(x => x.GetPagedAsync(ChatId, PageIndex, PageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);

            _mockMapper
                .Setup(x => x.Map<List<MessageServiceModel>>(It.IsAny<IEnumerable<Message>>()))
                .Returns([]);

            // Act
            await _service.GetPagedAsync(ChatId, PageIndex, PageSize);

            // Assert
            _mockLinkPreviewRepository.Verify(x => x.GetLinkPreviewsByIdAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GetPagedAsync_WithCancellationToken_ShouldPassTokenToRepositories()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var messages = new List<Message>
            {
                new() { Id = "msg-1", LinkPreviewId = LinkPreviewId }
            };

            _mockMessageRepository
                .Setup(x => x.GetPagedAsync(ChatId, PageIndex, PageSize, cancellationToken))
                .ReturnsAsync(messages);

            _mockLinkPreviewRepository
                .Setup(x => x.GetLinkPreviewsByIdAsync(It.IsAny<IEnumerable<string>>(), cancellationToken))
                .ReturnsAsync(new Dictionary<string, LinkPreview> { { LinkPreviewId, new LinkPreview { Id = LinkPreviewId } } });

            _mockMapper
                .Setup(x => x.Map<List<MessageServiceModel>>(It.IsAny<IEnumerable<Message>>()))
                .Returns([]);

            // Act
            await _service.GetPagedAsync(ChatId, PageIndex, PageSize, cancellationToken);

            // Assert
            _mockMessageRepository.Verify(x => x.GetPagedAsync(ChatId, PageIndex, PageSize, cancellationToken), Times.Once);
            _mockLinkPreviewRepository.Verify(x => x.GetLinkPreviewsByIdAsync(It.IsAny<IEnumerable<string>>(), cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class MarkAsReadAsyncTests : MessageCosmosDBServiceTests
    {
        [Test]
        public async Task MarkAsReadAsync_ShouldDelegateToStatusRepository()
        {
            // Arrange
            _mockChatMessageStatusRepository
                .Setup(x => x.MarkAsReadAsync(ChatId, SenderId, MessageId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.MarkAsReadAsync(ChatId, SenderId, MessageId, default);

            // Assert
            _mockChatMessageStatusRepository.Verify(x => x.MarkAsReadAsync(ChatId, SenderId, MessageId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task MarkAsReadAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockChatMessageStatusRepository
                .Setup(x => x.MarkAsReadAsync(ChatId, SenderId, MessageId, cancellationToken))
                .Returns(Task.CompletedTask);

            // Act
            await _service.MarkAsReadAsync(ChatId, SenderId, MessageId, cancellationToken);

            // Assert
            _mockChatMessageStatusRepository.Verify(x => x.MarkAsReadAsync(ChatId, SenderId, MessageId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class SaveImagePreviewAsyncTests : MessageCosmosDBServiceTests
    {
        [Test]
        public async Task SaveImagePreviewAsync_ShouldCallRepositoryAndReturnMappedResult()
        {
            // Arrange
            var message = new Message { Id = MessageId };
            var expectedModel = new MessageServiceModel { Id = MessageId };

            _mockMessageRepository
                .Setup(x => x.SetImagePreviewAsync(MessageId, ChatId, Filename, TestImageFormat, ImageWidth, ImageHeight, TestStorageType, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(message))
                .Returns(expectedModel);

            // Act
            var result = await _service.SaveImagePreviewAsync(MessageId, ChatId, Filename, TestImageFormat, ImageWidth, ImageHeight, TestStorageType);

            // Assert
            result.Should().BeEquivalentTo(expectedModel);
            _mockMessageRepository.Verify(x => x.SetImagePreviewAsync(MessageId, ChatId, Filename, TestImageFormat, ImageWidth, ImageHeight, TestStorageType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SaveImagePreviewAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var message = new Message { Id = MessageId };

            _mockMessageRepository
                .Setup(x => x.SetImagePreviewAsync(MessageId, ChatId, Filename, TestImageFormat, ImageWidth, ImageHeight, TestStorageType, cancellationToken))
                .ReturnsAsync(message);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(message))
                .Returns(new MessageServiceModel { Id = MessageId });

            // Act
            await _service.SaveImagePreviewAsync(MessageId, ChatId, Filename, TestImageFormat, ImageWidth, ImageHeight, TestStorageType, cancellationToken);

            // Assert
            _mockMessageRepository.Verify(x => x.SetImagePreviewAsync(MessageId, ChatId, Filename, TestImageFormat, ImageWidth, ImageHeight, TestStorageType, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class SetLinkPreviewAsyncWithIdTests : MessageCosmosDBServiceTests
    {
        [Test]
        public async Task SetLinkPreviewAsync_WithLinkPreviewId_ShouldUpdateMessageFetchPreviewAndReturnMapped()
        {
            // Arrange
            var message = new Message { Id = MessageId, LinkPreviewId = LinkPreviewId };
            var linkPreview = new LinkPreview { Id = LinkPreviewId, Url = Url };
            var expectedModel = new MessageServiceModel { Id = MessageId };
            var expectedPreviewModel = new LinkPreviewServiceModel { Url = Url };

            _mockMessageRepository
                .Setup(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);

            _mockLinkPreviewRepository
                .Setup(x => x.GetByIdAsync(LinkPreviewId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(linkPreview);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(message))
                .Returns(expectedModel);

            _mockMapper
                .Setup(x => x.Map<LinkPreviewServiceModel>(linkPreview))
                .Returns(expectedPreviewModel);

            // Act
            var result = await _service.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId);

            // Assert
            result.Should().BeEquivalentTo(expectedModel);
            result.LinkPreview.Should().BeEquivalentTo(expectedPreviewModel);
            _mockMessageRepository.Verify(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, It.IsAny<CancellationToken>()), Times.Once);
            _mockLinkPreviewRepository.Verify(x => x.GetByIdAsync(LinkPreviewId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SetLinkPreviewAsync_WithLinkPreviewId_WithCancellationToken_ShouldPassTokenToRepositories()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var message = new Message { Id = MessageId };
            var linkPreview = new LinkPreview { Id = LinkPreviewId };

            _mockMessageRepository
                .Setup(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, cancellationToken))
                .ReturnsAsync(message);

            _mockLinkPreviewRepository
                .Setup(x => x.GetByIdAsync(LinkPreviewId, cancellationToken))
                .ReturnsAsync(linkPreview);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(message))
                .Returns(new MessageServiceModel { Id = MessageId });

            // Act
            await _service.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, cancellationToken);

            // Assert
            _mockMessageRepository.Verify(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, cancellationToken), Times.Once);
            _mockLinkPreviewRepository.Verify(x => x.GetByIdAsync(LinkPreviewId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class SetLinkPreviewAsyncWithUrlTests : MessageCosmosDBServiceTests
    {
        [Test]
        public async Task SetLinkPreviewAsync_WithUrl_ShouldCreatePreviewUpdateMessageAndReturnMapped()
        {
            // Arrange
            var linkPreview = new LinkPreview { Id = LinkPreviewId, Url = Url, Title = Title, ImageUrl = ImageUrl };
            var message = new Message { Id = MessageId, LinkPreviewId = LinkPreviewId };
            var expectedModel = new MessageServiceModel { Id = MessageId };
            var expectedPreviewModel = new LinkPreviewServiceModel { Url = Url, Title = Title, ImageUrl = ImageUrl };

            _mockLinkPreviewRepository
                .Setup(x => x.CreateLinkPreviewAsync(Url, Title, ImageUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(linkPreview);

            _mockMessageRepository
                .Setup(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(message);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(message))
                .Returns(expectedModel);

            _mockMapper
                .Setup(x => x.Map<LinkPreviewServiceModel>(linkPreview))
                .Returns(expectedPreviewModel);

            // Act
            var result = await _service.SetLinkPreviewAsync(MessageId, ChatId, Url, Title, ImageUrl);

            // Assert
            result.Should().BeEquivalentTo(expectedModel);
            result.LinkPreview.Should().BeEquivalentTo(expectedPreviewModel);
            _mockLinkPreviewRepository.Verify(x => x.CreateLinkPreviewAsync(Url, Title, ImageUrl, It.IsAny<CancellationToken>()), Times.Once);
            _mockMessageRepository.Verify(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SetLinkPreviewAsync_WhenCreateReturnsNull_ShouldReturnNullAndNotUpdateMessage()
        {
            // Arrange
            _mockLinkPreviewRepository
                .Setup(x => x.CreateLinkPreviewAsync(Url, Title, ImageUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LinkPreview)null!);

            // Act
            var result = await _service.SetLinkPreviewAsync(MessageId, ChatId, Url, Title, ImageUrl);

            // Assert
            result.Should().BeNull();
            _mockMessageRepository.Verify(x => x.SetLinkPreviewAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task SetLinkPreviewAsync_WhenCreatedPreviewHasEmptyId_ShouldReturnNullAndNotUpdateMessage(string? emptyId)
        {
            // Arrange
            var linkPreview = new LinkPreview { Id = emptyId, Url = Url };

            _mockLinkPreviewRepository
                .Setup(x => x.CreateLinkPreviewAsync(Url, Title, ImageUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(linkPreview);

            // Act
            var result = await _service.SetLinkPreviewAsync(MessageId, ChatId, Url, Title, ImageUrl);

            // Assert
            result.Should().BeNull();
            _mockMessageRepository.Verify(x => x.SetLinkPreviewAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetLinkPreviewAsync_WithUrl_WithCancellationToken_ShouldPassTokenToRepositories()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var linkPreview = new LinkPreview { Id = LinkPreviewId };
            var message = new Message { Id = MessageId };

            _mockLinkPreviewRepository
                .Setup(x => x.CreateLinkPreviewAsync(Url, Title, ImageUrl, cancellationToken))
                .ReturnsAsync(linkPreview);

            _mockMessageRepository
                .Setup(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, cancellationToken))
                .ReturnsAsync(message);

            _mockMapper
                .Setup(x => x.Map<MessageServiceModel>(message))
                .Returns(new MessageServiceModel { Id = MessageId });

            // Act
            await _service.SetLinkPreviewAsync(MessageId, ChatId, Url, Title, ImageUrl, cancellationToken);

            // Assert
            _mockLinkPreviewRepository.Verify(x => x.CreateLinkPreviewAsync(Url, Title, ImageUrl, cancellationToken), Times.Once);
            _mockMessageRepository.Verify(x => x.SetLinkPreviewAsync(MessageId, ChatId, LinkPreviewId, cancellationToken), Times.Once);
        }
    }
}
