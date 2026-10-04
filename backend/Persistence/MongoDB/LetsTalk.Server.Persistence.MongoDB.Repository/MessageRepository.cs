using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Persistence.Enums;
using LetsTalk.Server.Persistence.MongoDB.Models;
using LetsTalk.Server.Persistence.MongoDB.Repository.Abstractions;
using LetsTalk.Server.DateHelpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LetsTalk.Server.Persistence.MongoDB.Repository;

public class MessageRepository : IMessageRepository
{
    private readonly IMongoCollection<Message> _messageCollection;
    
    private readonly IMongoCollection<LinkPreview> _linkPreviewCollection;

    public MessageRepository(
        IMongoClient mongoClient,
        IOptions<MongoDBSettings> mongoDBSettings)
    {
        var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);

        _messageCollection = mongoDatabase.GetCollection<Message>(nameof(Message));
        _linkPreviewCollection = mongoDatabase.GetCollection<LinkPreview>(nameof(LinkPreview));
    }

    public Task<List<Message>> GetPagedAsync(string chatId, int pageIndex, int messagesPerPage, CancellationToken cancellationToken = default)
    {
        return _messageCollection
            .AsQueryable()
            .Where(message => message.ChatId == chatId)
            .GroupJoin(_linkPreviewCollection, x => x.LinkPreviewId, x => x.Id, (message, linkPreviews) => new
            {
                Message = message,
                LinkPreviews = linkPreviews
            })
            .SelectMany(x => x.LinkPreviews.DefaultIfEmpty(), (g, linkPreview) => new Message
            {
                Id = g.Message.Id,
                Text = g.Message.Text,
                TextHtml = g.Message.TextHtml,
                SenderId = g.Message.SenderId,
                DateCreatedUnix = g.Message.DateCreatedUnix,
                LinkPreview = linkPreview,
                Image = g.Message.Image,
                ImagePreview = g.Message.ImagePreview,
                ChatId = g.Message.ChatId,
                EmojisOnly = g.Message.EmojisOnly,
                EmojiCount = g.Message.EmojiCount,
            })
            .OrderByDescending(message => message.DateCreatedUnix)
            .Skip(messagesPerPage * pageIndex)
            .Take(messagesPerPage)
            .OrderBy(message => message.DateCreatedUnix)
            .ToListAsync(cancellationToken);
    }

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
        var message = new Message
        {
            SenderId = senderId,
            ChatId = chatId,
            Text = text,
            TextHtml = textHtml,
            LinkPreviewId = linkPreviewId,
            DateCreatedUnix = DateHelper.GetUnixTimestamp(),
            EmojisOnly = emojisOnly,
            EmojiCount = emojiCount
        };

        await _messageCollection.InsertOneAsync(message, cancellationToken: cancellationToken);

        return message;
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
        var message = new Message
        {
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

        await _messageCollection.InsertOneAsync(message, cancellationToken: cancellationToken);

        return message;
    }

    public Task<Message> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _messageCollection.Aggregate()
            .Match(Builders<Message>.Filter.Eq(x => x.Id, id))
            .Lookup<Message, LinkPreview, Message>(
                foreignCollection: _linkPreviewCollection,
                localField: m => m.LinkPreviewId,
                foreignField: lp => lp.Id,
                @as: m => m.LinkPreview)
            .Unwind(m => m.LinkPreview, new AggregateUnwindOptions<Message>
            {
                PreserveNullAndEmptyArrays = true
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Message> SetLinkPreviewAsync(
        string messageId,
        string linkPreviewId,
        CancellationToken cancellationToken = default)
    {
        return _messageCollection.FindOneAndUpdateAsync(
            Builders<Message>.Filter.Eq(x => x.Id, messageId),
            Builders<Message>.Update.Set(x => x.LinkPreviewId, linkPreviewId),
            new FindOneAndUpdateOptions<Message, Message>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken: cancellationToken);
    }

    public Task<Message> SetImagePreviewAsync(
        string messageId,
        string filename,
        ImageFormats imageFormat,
        int width,
        int height,
        FileStorageTypes fileStorageType,
        CancellationToken cancellationToken = default)
    {
        return _messageCollection.FindOneAndUpdateAsync(
            Builders<Message>.Filter.Eq(x => x.Id, messageId),
            Builders<Message>.Update.Set(x => x.ImagePreview, new Image
            {
                Id = filename,
                ImageFormatId = (int)imageFormat,
                Width = width,
                Height = height,
                FileStorageTypeId = (int)fileStorageType
            }),
            new FindOneAndUpdateOptions<Message, Message>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken: cancellationToken);
    }
}
