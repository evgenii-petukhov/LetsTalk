using System.Net;
using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Resilience;
using Microsoft.Extensions.Time.Testing;

namespace LetsTalk.Server.Lambda.Tests;

// Both Lambdas have their own FakeHttpClientService (the name comes from the interface they share with the services).
// The retry rules are tested once, against RetryingHttpClient in Resilience.Tests; here only what is specific to a
// Lambda class: that it uses the factory, passes the callback and the settings on, and uses the defaults (spec 006, plan D12, AS4.1)
[TestFixture]
public class LambdaHttpClientTests
{
    private const string Url = "https://api.example/reports?token=secret-token";

    private static readonly string[] Lambdas = ["ImageProcessing", "LinkPreview"];

    private static Func<Action<RetryEvent>?, HttpRetrySettings?, HttpMessageHandler, TimeProvider, HttpClient> Factory(string lambda) =>
        lambda == "ImageProcessing"
            ? (onRetry, settings, inner, time) => new ImageProcessing.Lambda.FakeHttpClientService(onRetry, settings, inner, time).GetHttpClient()
            : (onRetry, settings, inner, time) => new LinkPreview.Lambda.FakeHttpClientService(onRetry, settings, inner, time).GetHttpClient();

    [TestCaseSource(nameof(Lambdas))]
    public async Task GetHttpClient_WhenCreatedWithTheDefaults_ShouldRepeatATransientAnswerThreeTimes(string lambda)
    {
        // Arrange: no settings are given, as in the deployed Lambda, so these are the shipped defaults
        var time = new FakeTimeProvider();
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable);
        using var client = Factory(lambda)(null, null, stub, time);

        // Act
        var response = await StubHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        stub.Calls.Should().Be(new HttpRetrySettings().MaxRetryAttempts + 1);
    }

    [TestCaseSource(nameof(Lambdas))]
    public async Task GetHttpClient_WhenRepeating_ShouldCallTheCallbackItWasGiven(string lambda)
    {
        // Arrange
        var time = new FakeTimeProvider();
        var events = new List<RetryEvent>();
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var client = Factory(lambda)(events.Add, null, stub, time);

        // Act
        await StubHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        events.Should().ContainSingle().Which.Reason.Should().Be("HTTP 503");
    }

    [TestCaseSource(nameof(Lambdas))]
    public async Task GetHttpClient_WhenSettingsAreGiven_ShouldUseThem(string lambda)
    {
        // Arrange
        var time = new FakeTimeProvider();
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable);
        using var client = Factory(lambda)(null, new HttpRetrySettings { MaxRetryAttempts = 1 }, stub, time);

        // Act
        await StubHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        stub.Calls.Should().Be(2);
    }
}
