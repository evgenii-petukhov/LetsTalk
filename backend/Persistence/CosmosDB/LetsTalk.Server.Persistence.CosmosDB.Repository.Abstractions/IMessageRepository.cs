using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.Enums;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;

public interface IMessageRepository
{
    Task<Message> CreateAsync(
        string senderId,
        string chatId,
        string text,
        string textHtml,
        bool emojisOnly,
        int emojiCount,
        string linkPreviewId,
        CancellationToken cancellationToken = default);

    Task<Message> CreateAsync(
        string senderId,
        string chatId,
        string text,
        string textHtml,
        string imageId,
        int width,
        int height,
        ImageFormats imageFormat,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default);

    Task<Message> GetByIdAsync(string messageId, string chatId, CancellationToken cancellationToken = default);

    Task<List<Message>> GetPagedAsync(
        string chatId,
        int pageIndex,
        int messagesPerPage,
        CancellationToken cancellationToken = default);

    Task<Message> SetImagePreviewAsync(
        string messageId,
        string chatId,
        string filename,
        ImageFormats imageFormat,
        int width,
        int height,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default);

    Task<Message> SetLinkPreviewAsync(
        string messageId,
        string chatId,
        string linkPreviewId,
        CancellationToken cancellationToken = default);
}
