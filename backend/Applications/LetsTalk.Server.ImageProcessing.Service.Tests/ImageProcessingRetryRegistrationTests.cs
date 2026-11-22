using System.Net;
using FluentAssertions;
using LetsTalk.Server.ImageProcessing.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.ImageProcessing.Service.Tests;

[TestFixture]
public class ImageProcessingRetryRegistrationTests
{
    private const string ApiUrl = "https://api.example/";

    [Test]
    public async Task AddImageProcessingServiceServices_ShouldRetryTheApiCallbackClient()
    {
        // Arrange: the pauses are the shortest the validator allows, so the test does not wait
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var provider = Build(stub);

        // Act
        var response = await provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(ImageResizeRequestConsumer))
            .PatchAsync(ApiUrl, new StringContent("{}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
    }

    [Test]
    public async Task AddImageProcessingServiceServices_WhenMaxRetryAttemptsIsConfigured_ShouldUseIt()
    {
        // Arrange
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable);
        using var provider = Build(stub, ("Resilience:Http:MaxRetryAttempts", "1"));

        // Act
        await provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ImageResizeRequestConsumer)).GetAsync(ApiUrl);

        // Assert
        stub.Calls.Should().Be(2);
    }

    [Test]
    public void AddImageProcessingServiceServices_WhenRetrySettingsAreWrong_ShouldFailToStart()
    {
        // Arrange
        using var provider = Build(new StubHandler(HttpStatusCode.OK), ("Resilience:Http:Delay", "00:00:00"));

        // Act
        var act = () => provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ImageResizeRequestConsumer))
            .GetAsync(ApiUrl).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<Exception>().WithMessage("*Resilience:Http:Delay*");
    }

    private static ServiceProvider Build(StubHandler stub, params (string Key, string Value)[] extra)
    {
        var values = new Dictionary<string, string?>
        {
            ["Resilience:Http:Delay"] = "00:00:00.100",
            ["Resilience:Http:MaxDelay"] = "00:00:00.100",
            ["ApplicationUrls:Api"] = ApiUrl
        };
        foreach (var (key, value) in extra)
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddImageProcessingServiceServices(configuration);
        services.AddHttpClient(nameof(ImageResizeRequestConsumer)).ConfigurePrimaryHttpMessageHandler(() => stub);
        return services.BuildServiceProvider();
    }
}
