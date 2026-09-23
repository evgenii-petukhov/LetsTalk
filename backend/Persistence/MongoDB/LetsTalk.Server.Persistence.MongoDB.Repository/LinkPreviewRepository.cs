using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Persistence.MongoDB.Models;
using LetsTalk.Server.Persistence.MongoDB.Repository.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LetsTalk.Server.Persistence.MongoDB.Repository;

public class LinkPreviewRepository : ILinkPreviewRepository
{
    private readonly IMongoCollection<LinkPreview> _linkPreviewCollection;

    public LinkPreviewRepository(
        IMongoClient mongoClient,
        IOptions<MongoDBSettings> mongoDBSettings)
    {
        var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);

        _linkPreviewCollection = mongoDatabase.GetCollection<LinkPreview>(nameof(LinkPreview));
    }

    public Task<string?> GetIdByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return _linkPreviewCollection
            .Find(Builders<LinkPreview>.Filter.Eq(x => x.Url, url))
            .Project(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<LinkPreview> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _linkPreviewCollection
            .Find(Builders<LinkPreview>.Filter.Eq(x => x.Id, id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LinkPreview> CreateLinkPreviewAsync(
        string url,
        string title,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        var linkPreview = new LinkPreview
        {
            Url = url,
            Title = title,
            ImageUrl = imageUrl
        };

        await _linkPreviewCollection.InsertOneAsync(linkPreview, cancellationToken: cancellationToken);

        return linkPreview;
    }
}
