using LetsTalk.Server.DateHelpers;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository;

public class MessageRepository(
    [FromKeyedServices(nameof(Message))] Container messageContainer) : IMessageRepository
{
    private readonly Container _messageContainer = messageContainer;

    public async Task<Message> CreateAsync(
        string senderId,
        string chatId,
        string text,
        string textHtml,
        bool emojisOnly,
        int emojiCount,
        string linkPreviewId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(textHtml);

        var message = new Message
        {
            Id = Guid.CreateVersion7().ToString("N"),
            SenderId = senderId,
            ChatId = chatId,
            Text = text,
            TextHtml = textHtml,
            LinkPreviewId = linkPreviewId,
            DateCreatedUnix = DateHelper.GetUnixTimestamp(),
            EmojisOnly = emojisOnly,
            EmojiCount = emojiCount
        };

        var response = await _messageContainer.CreateItemAsync(
            message,
            new PartitionKey(chatId),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<Message> CreateAsync(
        string senderId,
        string chatId,
        string text,
        string textHtml,
        string imageId,
        int width,
        int height,
        ImageFormats imageFormat,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(imageId);

        var message = new Message
        {
            Id = Guid.CreateVersion7().ToString("N"),
            SenderId = senderId,
            ChatId = chatId,
            Text = text,
            TextHtml = textHtml,
            DateCreatedUnix = DateHelper.GetUnixTimestamp(),
            Image = new Image
            {
                Id = imageId,
                ImageFormatId = (int)imageFormat,
                Width = width,
                Height = height,
                FileStorageTypeId = (int)fileStorageType
            }
        };

        var response = await _messageContainer.CreateItemAsync(
            message,
            new PartitionKey(chatId),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<Message> GetByIdAsync(
        string messageId,
        string chatId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);

        try
        {
            var response = await _messageContainer.ReadItemAsync<Message>(
                messageId,
                new PartitionKey(chatId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null!;
        }
    }

    public async Task<List<Message>> GetPagedAsync(
        string chatId,
        int pageIndex,
        int messagesPerPage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(messagesPerPage);

        var messageQuery = new QueryDefinition(
            "SELECT * " +
            "FROM c " +
            "WHERE c.chatId = @chatId " +
            "ORDER BY c.dateCreatedUnix DESC " +
            "OFFSET @offset LIMIT @limit")
            .WithParameter("@chatId", chatId)
            .WithParameter("@offset", messagesPerPage * pageIndex)
            .WithParameter("@limit", messagesPerPage);

        using FeedIterator<Message> messageIterator = _messageContainer.GetItemQueryIterator<Message>(
                messageQuery,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(chatId),
                    MaxItemCount = messagesPerPage
                });

        var messages = new List<Message>();

        while (messageIterator.HasMoreResults)
        {
            FeedResponse<Message> page = await messageIterator.ReadNextAsync(cancellationToken);
            messages.AddRange(page);
        }

        return messages;
    }

    public async Task<Message> SetImagePreviewAsync(
        string messageId,
        string chatId,
        string filename,
        ImageFormats imageFormat,
        int width,
        int height,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set("/imagePreview", new Image
            {
                Id = filename,
                ImageFormatId = (int)imageFormat,
                Width = width,
                Height = height,
                FileStorageTypeId = (int)fileStorageType
            })
        };

        var response = await _messageContainer.PatchItemAsync<Message>(
            messageId,
            new PartitionKey(chatId),
            patchOperations,
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<Message> SetLinkPreviewAsync(
        string messageId,
        string chatId,
        string linkPreviewId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(linkPreviewId);

        var patchOperations = new List<PatchOperation>
        {
            PatchOperation.Set("/linkPreviewId", linkPreviewId)
        };

        var messageResponse = await _messageContainer.PatchItemAsync<Message>(
            messageId,
            new PartitionKey(chatId),
            patchOperations,
            cancellationToken: cancellationToken);

        return messageResponse.Resource;
    }

    public async Task<List<Message>> GetMessagesByChatIdsAsync(
        IReadOnlyList<string> chatIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatIds);

        var ids = chatIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        var inClause = string.Join(",", ids.Select((_, i) => $"@C{i}"));
        var query = new QueryDefinition(
            "SELECT c.id, c.chatId, c.senderId, c.dateCreatedUnix " +
            "FROM c " +
            $"WHERE c.chatId IN ({inClause})");
        for (int i = 0; i < ids.Length; i++)
        {
            query = query.WithParameter($"@C{i}", ids[i]);
        }    

        using var iterator = _messageContainer.GetItemQueryIterator<Message>(query);
        var messages = new List<Message>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            messages.AddRange(page);
        }
        return messages;
    }
}
