using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository;

public class LinkPreviewRepository(
    [FromKeyedServices(nameof(LinkPreview))] Container container) : ILinkPreviewRepository
{
    private readonly Container _container = container;

    public async Task<string?> GetIdByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 * FROM c WHERE c.url = @url")
            .WithParameter("@url", url);

        using var iterator = _container.GetItemQueryIterator<LinkPreview>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 1
            });

        if (!iterator.HasMoreResults)
        {
            return null;
        }

        var page = await iterator.ReadNextAsync(cancellationToken);

        return page.FirstOrDefault()?.Id;
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
            "SELECT TOP 1 * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);

        using var iterator = _container.GetItemQueryIterator<LinkPreview>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = 1
            });

        if (!iterator.HasMoreResults)
        {
            return null!;
        }

        var page = await iterator.ReadNextAsync(cancellationToken);

        return page.FirstOrDefault()!;
    }
}
