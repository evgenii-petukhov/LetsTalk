using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository;

public class LinkPreviewRepository(
    [FromKeyedServices(nameof(LinkPreview))] Container container) : ILinkPreviewRepository
{
    private readonly Container _container = container;

    public async Task<string?> GetIdByUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var query = new QueryDefinition(
            "SELECT TOP 1 VALUE c.id " +
            "FROM c " +
            "WHERE c.url = @url")
            .WithParameter("@url", url);

        using var iterator = _container.GetItemQueryIterator<string>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 1,
                PartitionKey = new PartitionKey(url)
            });

        var page = await iterator.ReadNextAsync(cancellationToken);

        return page.FirstOrDefault();
    }

    public async Task<LinkPreview> CreateLinkPreviewAsync(
        string url,
        string title,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        var account = new LinkPreview
        {
            Id = Guid.CreateVersion7().ToString("N"),
            Title = title,
            ImageUrl = imageUrl,
            Url = url
        };

        var response = await _container.CreateItemAsync(
            account,
            new PartitionKey(url),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<LinkPreview> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 * " +
            "FROM c " +
            "WHERE c.id = @id")
            .WithParameter("@id", id);

        using var iterator = _container.GetItemQueryIterator<LinkPreview>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 1
            });

        var page = await iterator.ReadNextAsync(cancellationToken);

        return page.FirstOrDefault()!;
    }

    public async Task<Dictionary<string, LinkPreview>> GetLinkPreviewsByIdAsync(
        IEnumerable<string> linkPreviewIds,
        CancellationToken cancellationToken = default)
    {
        var ids = linkPreviewIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        var linkPreviewIdArray = linkPreviewIds.ToArray();

        var linkPreviewQuery = new QueryDefinition(
            "SELECT * " +
            "FROM c " +
            "WHERE ARRAY_CONTAINS(@linkPreviewIds, c.id)")
            .WithParameter("@linkPreviewIds", linkPreviewIdArray);

        using FeedIterator<LinkPreview> linkPreviewIterator =
            _container.GetItemQueryIterator<LinkPreview>(
                linkPreviewQuery,
                requestOptions: new QueryRequestOptions
                {
                    MaxItemCount = linkPreviewIdArray.Length
                });

        var linkPreviews = new Dictionary<string, LinkPreview>(StringComparer.Ordinal);

        while (linkPreviewIterator.HasMoreResults)
        {
            FeedResponse<LinkPreview> page = await linkPreviewIterator.ReadNextAsync(cancellationToken);

            foreach (LinkPreview linkPreview in page)
            {
                if (!string.IsNullOrWhiteSpace(linkPreview.Id))
                {
                    linkPreviews[linkPreview.Id] = linkPreview;
                }
            }
        }

        return linkPreviews;
    }
}
