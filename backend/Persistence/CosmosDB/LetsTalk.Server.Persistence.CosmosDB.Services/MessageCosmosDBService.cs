using AutoMapper;
using LetsTalk.Server.Persistence.AgnosticServices.Abstractions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;

namespace LetsTalk.Server.Persistence.CosmosDB.Services;

public class MessageCosmosDBService(
    IMessageRepository messageRepository,
    IChatMessageStatusRepository chatMessageStatusRepository,
    ILinkPreviewRepository linkPreviewRepository,
    IMapper mapper) : IMessageAgnosticService
{
    private readonly IMessageRepository _messageRepository = messageRepository;
    private readonly IChatMessageStatusRepository _chatMessageStatusRepository = chatMessageStatusRepository;
    private readonly ILinkPreviewRepository _linkPreviewRepository = linkPreviewRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<MessageServiceModel> CreateMessageAsync(
        string senderId,
        string chatId,
        string text,
        string textHtml,
        bool emojisOnly,
        int emojiCount,
        string linkPreviewId,
        CancellationToken cancellationToken)
    {
        var message = await _messageRepository.CreateAsync(
            senderId,
            chatId,
            text,
            textHtml,
            emojisOnly,
            emojiCount,
            linkPreviewId,
            cancellationToken);

        var mappedMessage = _mapper.Map<MessageServiceModel>(message);

        var linkPreview = await _linkPreviewRepository.GetByIdAsync(linkPreviewId, cancellationToken);
        mappedMessage.LinkPreview = _mapper.Map<LinkPreviewServiceModel>(linkPreview);

        return mappedMessage;
    }

    public async Task<MessageServiceModel> CreateMessageAsync(
        string senderId,
        string chatId,
        string text,
        string textHtml,
        string imageId,
        int width,
        int height,
        ImageFormats imageFormat,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken)
    {
        var message = await _messageRepository.CreateAsync(
            senderId,
            chatId,
            text,
            textHtml,
            imageId,
            width,
            height,
            imageFormat,
            fileStorageType,
            cancellationToken);

        return _mapper.Map<MessageServiceModel>(message);
    }

    public async Task<List<MessageServiceModel>> GetPagedAsync(
        string chatId,
        int pageIndex,
        int messagesPerPage,
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageRepository.GetPagedAsync(
            chatId,
            pageIndex,
            messagesPerPage,
            cancellationToken);

        return _mapper.Map<List<MessageServiceModel>>(messages);
    }

    public Task MarkAsReadAsync(
        string chatId,
        string accountId,
        string messageId,
        CancellationToken cancellationToken)
    {
        return _chatMessageStatusRepository.MarkAsReadAsync(chatId, accountId, messageId, cancellationToken);
    }

    public async Task<MessageServiceModel> SaveImagePreviewAsync(
        string messageId,
        string chatId,
        string filename,
        ImageFormats imageFormat,
        int width,
        int height,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.SetImagePreviewAsync(
            messageId,
            chatId,
            filename,
            imageFormat,
            width,
            height,
            fileStorageType,
            cancellationToken);

        return _mapper.Map<MessageServiceModel>(message);
    }

    public async Task<MessageServiceModel> SetLinkPreviewAsync(
        string messageId,
        string chatId,
        string linkPreviewId,
        CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.SetLinkPreviewAsync(messageId, chatId, linkPreviewId, cancellationToken);

        var mappedMessage = _mapper.Map<MessageServiceModel>(message);

        var linkPreview = await _linkPreviewRepository.GetByIdAsync(linkPreviewId, cancellationToken);
        mappedMessage.LinkPreview = _mapper.Map<LinkPreviewServiceModel>(linkPreview);

        return mappedMessage;
    }

    public async Task<MessageServiceModel> SetLinkPreviewAsync(
        string messageId,
        string chatId,
        string url,
        string title,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        var linkPreview = await _linkPreviewRepository.CreateLinkPreviewAsync(url, title, imageUrl, cancellationToken);

        if (linkPreview == null || string.IsNullOrWhiteSpace(linkPreview.Id))
        {
            return null!;
        }

        var message = await _messageRepository.SetLinkPreviewAsync(messageId, chatId, linkPreview.Id, cancellationToken);

        var mappedMessage = _mapper.Map<MessageServiceModel>(message);
        mappedMessage.LinkPreview = _mapper.Map<LinkPreviewServiceModel>(linkPreview);

        return mappedMessage;
    }
}
