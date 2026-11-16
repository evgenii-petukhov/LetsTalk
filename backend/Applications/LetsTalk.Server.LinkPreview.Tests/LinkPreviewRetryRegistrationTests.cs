using System.Net;
using FluentAssertions;
using LetsTalk.Server.LinkPreview.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.LinkPreview.Tests;

[TestFixture]
public class LinkPreviewRetryRegistrationTests
{
    private const string ApiUrl = "https://api.example/";
    private const string PageUrl = "https://page.example/article";

    private static readonly string[] BothClients = [LinkPreviewRequestConsumer.ApiClientName, nameof(HttpClientService)];

    [TestCaseSource(nameof(BothClients))]
    public async Task AddLinkPreviewServices_ShouldRetryTheClient(string clientName)
    {
        // Arrange: the pauses are the shortest the validator allows, so the test does not wait
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var provider = Build((clientName, stub));

        // Act
        var response = await provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName).GetAsync(PageUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
    }

    [Test]
    public void AddLinkPreviewServices_ShouldRegisterTheApiCallbackAndTheDownloadAsTwoClients()
    {
        // Arrange
        var api = new StubHandler(HttpStatusCode.OK);
        var download = new StubHandler(HttpStatusCode.OK);
        using var provider = Build((LinkPreviewRequestConsumer.ApiClientName, api), (nameof(HttpClientService), download));
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act
        factory.CreateClient(LinkPreviewRequestConsumer.ApiClientName).GetAsync(ApiUrl).GetAwaiter().GetResult();

        // Assert: a failing site never changes the client that reports to the API (spec FR-006)
        LinkPreviewRequestConsumer.ApiClientName.Should().NotBe(nameof(HttpClientService));
        api.Calls.Should().Be(1);
        download.Calls.Should().Be(0);
    }

    [Test]
    public async Task AddLinkPreviewServices_WhenOnePageKeepsFailing_ShouldNotSlowTheApiCallback()
    {
        // Arrange
        var api = new StubHandler(HttpStatusCode.OK);
        var download = new StubHandler(HttpStatusCode.ServiceUnavailable);
        using var provider = Build((LinkPreviewRequestConsumer.ApiClientName, api), (nameof(HttpClientService), download));
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act
        await factory.CreateClient(nameof(HttpClientService)).GetAsync(PageUrl);
        var response = await factory.CreateClient(LinkPreviewRequestConsumer.ApiClientName).GetAsync(ApiUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        api.Calls.Should().Be(1);
        download.Calls.Should().Be(4);
    }

    [Test]
    public void AddLinkPreviewServices_WhenRetrySettingsAreWrong_ShouldFailToStart()
    {
        // Arrange
        using var provider = Build(
            new Dictionary<string, string?> { ["Resilience:Http:Delay"] = "00:00:00" },
            (LinkPreviewRequestConsumer.ApiClientName, new StubHandler(HttpStatusCode.OK)));

        // Act
        var act = () => provider.GetRequiredService<IHttpClientFactory>().CreateClient(LinkPreviewRequestConsumer.ApiClientName)
            .GetAsync(ApiUrl).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<Exception>().WithMessage("*Resilience:Http:Delay*");
    }

    private static ServiceProvider Build(params (string Name, StubHandler Handler)[] clients) => Build(null, clients);

    private static ServiceProvider Build(Dictionary<string, string?>? extra, params (string Name, StubHandler Handler)[] clients)
    {
        var values = new Dictionary<string, string?>
        {
            ["Resilience:Http:Delay"] = "00:00:00.100",
            ["Resilience:Http:MaxDelay"] = "00:00:00.100",
            ["ApplicationUrls:Api"] = ApiUrl
        };
        foreach (var (key, value) in extra ?? [])
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLinkPreviewServices(configuration);
        foreach (var (name, handler) in clients)
        {
            services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        return services.BuildServiceProvider();
    }
}
