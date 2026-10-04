using FluentAssertions;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class LinkPreviewCosmosDBServiceTests
{
    private const string Url = "https://example.com/page";
    private const string LinkPreviewId = "preview-001";

    private Mock<ILinkPreviewRepository> _mockLinkPreviewRepository;
    private LinkPreviewCosmosDBService _service;

    [SetUp]
    public void SetUp()
    {
        _mockLinkPreviewRepository = new Mock<ILinkPreviewRepository>();
        _service = new LinkPreviewCosmosDBService(_mockLinkPreviewRepository.Object);
    }

    [TestFixture]
    public class GetIdByUrlAsyncTests : LinkPreviewCosmosDBServiceTests
    {
        [Test]
        public async Task GetIdByUrlAsync_WhenUrlExists_ShouldReturnId()
        {
            // Arrange
            _mockLinkPreviewRepository
                .Setup(x => x.GetIdByUrlAsync(Url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(LinkPreviewId);

            // Act
            var result = await _service.GetIdByUrlAsync(Url);

            // Assert
            result.Should().Be(LinkPreviewId);
            _mockLinkPreviewRepository.Verify(x => x.GetIdByUrlAsync(Url, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetIdByUrlAsync_WhenUrlDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            _mockLinkPreviewRepository
                .Setup(x => x.GetIdByUrlAsync(Url, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _service.GetIdByUrlAsync(Url);

            // Assert
            result.Should().BeNull();
            _mockLinkPreviewRepository.Verify(x => x.GetIdByUrlAsync(Url, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetIdByUrlAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _mockLinkPreviewRepository
                .Setup(x => x.GetIdByUrlAsync(Url, cancellationToken))
                .ReturnsAsync(LinkPreviewId);

            // Act
            var result = await _service.GetIdByUrlAsync(Url, cancellationToken);

            // Assert
            result.Should().Be(LinkPreviewId);
            _mockLinkPreviewRepository.Verify(x => x.GetIdByUrlAsync(Url, cancellationToken), Times.Once);
        }
    }
}
