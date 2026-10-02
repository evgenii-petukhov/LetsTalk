using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.CosmosDB.Services;
using LetsTalk.Server.Persistence.Enums;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class MessageCosmosDBServiceTests
{
    private Mock<IMessageRepository> _messageRepository = null!;
    private Mock<IChatMessageStatusRepository> _statusRepository = null!;
    private Mock<ILinkPreviewRepository> _linkPreviewRepository = null!;
    private Mock<IMapper> _mapper = null!;
    private MessageCosmosDBService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _messageRepository = new Mock<IMessageRepository>();
        _statusRepository = new Mock<IChatMessageStatusRepository>();
        _linkPreviewRepository = new Mock<ILinkPreviewRepository>();
        _mapper = new Mock<IMapper>();
        _service = new MessageCosmosDBService(
            _messageRepository.Object,
            _statusRepository.Object,
            _linkPreviewRepository.Object,
            _mapper.Object);
    }

    [Test]
    public async Task CreateMessageAsync_WithLinkPreview_ShouldCreateMapAndAttachPreview()
    {
        const string senderId = "account-1";
        const string chatId = "chat-1";
        const string linkPreviewId = "preview-1";
        var cancellationToken = new CancellationToken(canceled: true);
        var message = new Message { Id = "message-1" };
        var preview = new LinkPreview { Id = linkPreviewId, Url = "https://example.com" };
        var mappedMessage = new MessageServiceModel { Id = message.Id };
        var mappedPreview = new LinkPreviewServiceModel { Url = preview.Url };
        _messageRepository
            .Setup(x => x.CreateAsync(senderId, chatId, "text", "<p>text</p>", false, 0, linkPreviewId, cancellationToken))
            .ReturnsAsync(message);
        _linkPreviewRepository.Setup(x => x.GetByIdAsync(linkPreviewId, cancellationToken)).ReturnsAsync(preview);
        _mapper.Setup(x => x.Map<MessageServiceModel>(message)).Returns(mappedMessage);
        _mapper.Setup(x => x.Map<LinkPreviewServiceModel>(preview)).Returns(mappedPreview);

        var result = await _service.CreateMessageAsync(
            senderId, chatId, "text", "<p>text</p>", false, 0, linkPreviewId, cancellationToken);

        result.Should().BeSameAs(mappedMessage);
        result.LinkPreview.Should().BeSameAs(mappedPreview);
        _messageRepository.Verify(
            x => x.CreateAsync(senderId, chatId, "text", "<p>text</p>", false, 0, linkPreviewId, cancellationToken),
            Times.Once);
        _linkPreviewRepository.Verify(x => x.GetByIdAsync(linkPreviewId, cancellationToken), Times.Once);
    }

    [Test]
    public async Task CreateMessageAsync_WithImage_ShouldReturnMappedMessage()
    {
        const string senderId = "account-1";
        const string chatId = "chat-1";
        const string imageId = "image-1";
        const int width = 640;
        const int height = 480;
        const ImageFormats imageFormat = ImageFormats.Jpeg;
        const FileStorageTypes fileStorageType = FileStorageTypes.AmazonS3;
        var cancellationToken = new CancellationToken(canceled: true);
        var message = new Message { Id = "message-1" };
        var expected = new MessageServiceModel { Id = message.Id };
        _messageRepository
            .Setup(x => x.CreateAsync(senderId, chatId, "text", "<p>text</p>", imageId, width, height, imageFormat, fileStorageType, cancellationToken))
            .ReturnsAsync(message);
        _mapper.Setup(x => x.Map<MessageServiceModel>(message)).Returns(expected);

        var result = await _service.CreateMessageAsync(
            senderId,
            chatId,
            "text",
            "<p>text</p>",
            imageId,
            width,
            height,
            imageFormat,
            fileStorageType,
            cancellationToken);

        result.Should().BeSameAs(expected);
        _messageRepository.Verify(
            x => x.CreateAsync(senderId, chatId, "text", "<p>text</p>", imageId, width, height, imageFormat, fileStorageType, cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task GetPagedAsync_ShouldBatchDistinctPreviewsAndMapMessagesInAscendingDateOrder()
    {
        const string chatId = "chat-1";
        var cancellationToken = new CancellationToken(canceled: true);
        var newest = new Message { Id = "message-3", DateCreatedUnix = 30, LinkPreviewId = "preview-1" };
        var earliest = new Message { Id = "message-1", DateCreatedUnix = 10, LinkPreviewId = " " };
        var middle = new Message { Id = "message-2", DateCreatedUnix = 20, LinkPreviewId = "preview-1" };
        var missingPreview = new Message { Id = "message-4", DateCreatedUnix = 40, LinkPreviewId = "missing-preview" };
        var messages = new List<Message> { newest, earliest, middle, missingPreview };
        var preview = new LinkPreview { Id = "preview-1" };
        var expected = new List<MessageServiceModel> { new() { Id = "message-1" }, new() { Id = "message-2" }, new() { Id = "message-3" }, new() { Id = "message-4" } };
        List<Message>? mappedMessages = null;
        _messageRepository.Setup(x => x.GetPagedAsync(chatId, 2, 25, cancellationToken)).ReturnsAsync(messages);
        _linkPreviewRepository
            .Setup(x => x.GetLinkPreviewsByIdAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "preview-1", "missing-preview" })),
                cancellationToken))
            .ReturnsAsync(new Dictionary<string, LinkPreview> { ["preview-1"] = preview });
        _mapper
            .Setup(x => x.Map<List<MessageServiceModel>>(It.IsAny<object>()))
            .Callback<object>(source => mappedMessages = ((IEnumerable<Message>)source).ToList())
            .Returns(expected);

        var result = await _service.GetPagedAsync(chatId, 2, 25, cancellationToken);

        result.Should().BeSameAs(expected);
        mappedMessages!.Select(x => x.Id).Should().Equal("message-1", "message-2", "message-3", "message-4");
        earliest.LinkPreview.Should().BeNull();
        middle.LinkPreview.Should().BeSameAs(preview);
        newest.LinkPreview.Should().BeSameAs(preview);
        missingPreview.LinkPreview.Should().BeNull();
        _messageRepository.Verify(x => x.GetPagedAsync(chatId, 2, 25, cancellationToken), Times.Once);
        _linkPreviewRepository.Verify(x => x.GetLinkPreviewsByIdAsync(It.IsAny<IEnumerable<string>>(), cancellationToken), Times.Once);
    }

    [Test]
    public async Task GetPagedAsync_WhenNoMessagesHavePreviewIds_ShouldSkipPreviewLookup()
    {
        const string chatId = "chat-1";
        var messages = new List<Message> { new() { Id = "message-1", DateCreatedUnix = 1 } };
        var expected = new List<MessageServiceModel> { new() { Id = "message-1" } };
        _messageRepository.Setup(x => x.GetPagedAsync(chatId, 0, 10, It.IsAny<CancellationToken>())).ReturnsAsync(messages);
        _mapper.Setup(x => x.Map<List<MessageServiceModel>>(It.IsAny<object>())).Returns(expected);

        var result = await _service.GetPagedAsync(chatId, 0, 10);

        result.Should().BeSameAs(expected);
        _linkPreviewRepository.Verify(x => x.GetLinkPreviewsByIdAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task MarkAsReadAsync_ShouldForwardArgumentsAndCancellationToken()
    {
        const string chatId = "chat-1";
        const string accountId = "account-1";
        const string messageId = "message-1";
        var cancellationToken = new CancellationToken(canceled: true);

        await _service.MarkAsReadAsync(chatId, accountId, messageId, cancellationToken);

        _statusRepository.Verify(x => x.MarkAsReadAsync(chatId, accountId, messageId, cancellationToken), Times.Once);
    }

    [Test]
    public async Task SaveImagePreviewAsync_ShouldSaveAndMapMessage()
    {
        const string messageId = "message-1";
        const string chatId = "chat-1";
        const string filename = "preview.jpg";
        const int width = 320;
        const int height = 240;
        const ImageFormats imageFormat = ImageFormats.Jpeg;
        const FileStorageTypes fileStorageType = FileStorageTypes.AmazonS3;
        var cancellationToken = new CancellationToken(canceled: true);
        var message = new Message { Id = messageId };
        var expected = new MessageServiceModel { Id = messageId };
        _messageRepository
            .Setup(x => x.SetImagePreviewAsync(messageId, chatId, filename, imageFormat, width, height, fileStorageType, cancellationToken))
            .ReturnsAsync(message);
        _mapper.Setup(x => x.Map<MessageServiceModel>(message)).Returns(expected);

        var result = await _service.SaveImagePreviewAsync(
            messageId, chatId, filename, imageFormat, width, height, fileStorageType, cancellationToken);

        result.Should().BeSameAs(expected);
        _messageRepository.Verify(
            x => x.SetImagePreviewAsync(messageId, chatId, filename, imageFormat, width, height, fileStorageType, cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task SetLinkPreviewAsync_WithPreviewId_ShouldAttachAndMapPreview()
    {
        const string messageId = "message-1";
        const string chatId = "chat-1";
        const string previewId = "preview-1";
        var cancellationToken = new CancellationToken(canceled: true);
        var message = new Message { Id = messageId };
        var preview = new LinkPreview { Id = previewId };
        var expected = new MessageServiceModel { Id = messageId };
        var mappedPreview = new LinkPreviewServiceModel { Url = "https://example.com" };
        _messageRepository.Setup(x => x.SetLinkPreviewAsync(messageId, chatId, previewId, cancellationToken)).ReturnsAsync(message);
        _linkPreviewRepository.Setup(x => x.GetByIdAsync(previewId, cancellationToken)).ReturnsAsync(preview);
        _mapper.Setup(x => x.Map<MessageServiceModel>(message)).Returns(expected);
        _mapper.Setup(x => x.Map<LinkPreviewServiceModel>(preview)).Returns(mappedPreview);

        var result = await _service.SetLinkPreviewAsync(messageId, chatId, previewId, cancellationToken);

        result.Should().BeSameAs(expected);
        result.LinkPreview.Should().BeSameAs(mappedPreview);
        _messageRepository.Verify(x => x.SetLinkPreviewAsync(messageId, chatId, previewId, cancellationToken), Times.Once);
        _linkPreviewRepository.Verify(x => x.GetByIdAsync(previewId, cancellationToken), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task SetLinkPreviewAsync_WithMissingOrBlankCreatedPreviewId_ShouldReturnNullWithoutSettingMessage(string? createdPreviewId)
    {
        const string messageId = "message-1";
        const string chatId = "chat-1";
        const string url = "https://example.com";
        var cancellationToken = new CancellationToken(canceled: true);
        var preview = createdPreviewId is null ? null : new LinkPreview { Id = createdPreviewId };
        _linkPreviewRepository
            .Setup(x => x.CreateLinkPreviewAsync(url, "title", "image-url", cancellationToken))
            .ReturnsAsync(preview!);

        var result = await _service.SetLinkPreviewAsync(messageId, chatId, url, "title", "image-url", cancellationToken);

        result.Should().BeNull();
        _messageRepository.Verify(
            x => x.SetLinkPreviewAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mapper.Verify(x => x.Map<MessageServiceModel>(It.IsAny<object>()), Times.Never);
    }

    [Test]
    public async Task SetLinkPreviewAsync_WithUrl_ShouldCreateAttachAndMapPreview()
    {
        const string messageId = "message-1";
        const string chatId = "chat-1";
        const string url = "https://example.com";
        var cancellationToken = new CancellationToken(canceled: true);
        var preview = new LinkPreview { Id = "preview-1", Url = url };
        var message = new Message { Id = messageId };
        var expected = new MessageServiceModel { Id = messageId };
        var mappedPreview = new LinkPreviewServiceModel { Url = url };
        _linkPreviewRepository
            .Setup(x => x.CreateLinkPreviewAsync(url, "title", "image-url", cancellationToken))
            .ReturnsAsync(preview);
        _messageRepository.Setup(x => x.SetLinkPreviewAsync(messageId, chatId, preview.Id!, cancellationToken)).ReturnsAsync(message);
        _mapper.Setup(x => x.Map<MessageServiceModel>(message)).Returns(expected);
        _mapper.Setup(x => x.Map<LinkPreviewServiceModel>(preview)).Returns(mappedPreview);

        var result = await _service.SetLinkPreviewAsync(messageId, chatId, url, "title", "image-url", cancellationToken);

        result.Should().BeSameAs(expected);
        result.LinkPreview.Should().BeSameAs(mappedPreview);
        _linkPreviewRepository.Verify(x => x.CreateLinkPreviewAsync(url, "title", "image-url", cancellationToken), Times.Once);
        _messageRepository.Verify(x => x.SetLinkPreviewAsync(messageId, chatId, preview.Id!, cancellationToken), Times.Once);
    }
}
