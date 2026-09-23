using LetsTalk.Server.Persistence.MongoDB.Models;

namespace LetsTalk.Server.Persistence.MongoDB.Repository.Abstractions;

public interface ILinkPreviewRepository
{
    Task<string?> GetIdByUrlAsync(string url, CancellationToken cancellationToken = default);

    Task<LinkPreview> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<LinkPreview> CreateLinkPreviewAsync(
        string url,
        string title,
        string imageUrl,
        CancellationToken cancellationToken = default);
}
