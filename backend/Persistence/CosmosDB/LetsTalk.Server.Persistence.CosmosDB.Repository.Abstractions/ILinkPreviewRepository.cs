using LetsTalk.Server.Persistence.CosmosDB.Models;

namespace LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;

public interface ILinkPreviewRepository
{
    Task<string?> GetIdByUrlAsync(
        string url, 
        CancellationToken cancellationToken = default);

    Task<LinkPreview> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<LinkPreview> CreateLinkPreviewAsync(
        string url,
        string title,
        string imageUrl,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, LinkPreview>> GetLinkPreviewsByIdAsync(
        IEnumerable<string> linkPreviewIds,
        CancellationToken cancellationToken = default);
}
