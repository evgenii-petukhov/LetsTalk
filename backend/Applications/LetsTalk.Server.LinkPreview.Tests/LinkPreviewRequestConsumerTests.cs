using System.Net;
using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.LinkPreview.Utility.Abstractions;
using LetsTalk.Server.LinkPreview.Utility.Abstractions.Models;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using KafkaLinkPreviewRequest = LetsTalk.Server.Models.Kafka.LinkPreviewRequest;

namespace LetsTalk.Server.LinkPreview.Tests;

[TestFixture]
public class LinkPreviewRequestConsumerTests
{
    private const string ApiUrl = "https://api.example/";
    private const string PageUrl = "https://page.example/article";

    [Test]
    public async Task Consume_ShouldSendTheReportThroughTheApiCallbackClientOnly()
    {
        // Arrange
        var api = new StubHandler(HttpStatusCode.OK);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(LinkPreviewRequestConsumer.ApiClientName)).Returns(() => new HttpClient(api, false));

        var linkPreviewService = new Mock<ILinkPreviewService>();
        linkPreviewService.Setup(x => x.GenerateLinkPreviewAsync(It.IsAny<LinkPreviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenGraphModel { Title = "A title", ImageUrl = "https://page.example/a.png" });

        var consumer = new LinkPreviewRequestConsumer(
            linkPreviewService.Object,
            NullLogger<LinkPreviewRequestConsumer>.Instance,
            factory.Object,
            Options.Create(new ApplicationUrlSettings { Api = ApiUrl }),
            Options.Create(new LinkPreviewSettings()));

        var context = new Mock<ConsumeContext<KafkaLinkPreviewRequest>>();
        context.SetupGet(x => x.Message).Returns(new KafkaLinkPreviewRequest { Url = PageUrl, ChatId = "c1", MessageId = "m1", Token = "t" });
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        api.Requests.Should().ContainSingle().Which.Should().StartWith("PATCH https://api.example/");
        factory.Verify(x => x.CreateClient(LinkPreviewRequestConsumer.ApiClientName), Times.Once);
        factory.Verify(x => x.CreateClient(It.Is<string>(n => n != LinkPreviewRequestConsumer.ApiClientName)), Times.Never);
    }
}
