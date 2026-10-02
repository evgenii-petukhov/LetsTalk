using FluentAssertions;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.CosmosDB.Services;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class LinkPreviewCosmosDBServiceTests
{
    [Test]
    public async Task GetIdByUrlAsync_ShouldReturnRepositoryResultAndForwardCancellationToken()
    {
        const string url = "https://example.com/article";
        var cancellationToken = new CancellationToken(canceled: true);
        var repository = new Mock<ILinkPreviewRepository>();
        repository.Setup(x => x.GetIdByUrlAsync(url, cancellationToken)).ReturnsAsync("preview-1");
        var service = new LinkPreviewCosmosDBService(repository.Object);

        var result = await service.GetIdByUrlAsync(url, cancellationToken);

        result.Should().Be("preview-1");
        repository.Verify(x => x.GetIdByUrlAsync(url, cancellationToken), Times.Once);
    }

    [Test]
    public async Task GetIdByUrlAsync_WhenNoPreviewExists_ShouldReturnNull()
    {
        const string url = "https://example.com/missing";
        var repository = new Mock<ILinkPreviewRepository>();
        repository.Setup(x => x.GetIdByUrlAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var service = new LinkPreviewCosmosDBService(repository.Object);

        var result = await service.GetIdByUrlAsync(url);

        result.Should().BeNull();
        repository.Verify(x => x.GetIdByUrlAsync(url, It.IsAny<CancellationToken>()), Times.Once);
    }
}
