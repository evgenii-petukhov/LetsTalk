using System.Net;
using FluentAssertions;
using LetsTalk.Server.Resilience;
using Microsoft.Extensions.Time.Testing;

namespace LetsTalk.Server.Lambda.Tests;

// Both Lambdas have their own FakeHttpClientService (the name comes from the interface they share with the services);
// the same scenarios run against each, so the two cannot drift apart (spec 006, FR-008, AS4.1)
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
    public async Task GetHttpClient_WhenFirstAnswerIsTransient_ShouldReturnTheLaterAnswer(string lambda)
    {
        // Arrange
        var time = new FakeTimeProvider();
        var stub = new StubHandler(HttpStatusCode.BadGateway, HttpStatusCode.OK);
        using var client = Factory(lambda)(null, null, stub, time);

        // Act
        var response = await StubHandler.RunAsync(client.PatchAsync(Url, new StringContent("{}")), time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
    }

    [TestCaseSource(nameof(Lambdas))]
    public async Task GetHttpClient_WhenAnswerIsNotTransient_ShouldNotRepeat(string lambda)
    {
        // Arrange
        var time = new FakeTimeProvider();
        var stub = new StubHandler(HttpStatusCode.NotFound, HttpStatusCode.OK);
        using var client = Factory(lambda)(null, null, stub, time);

        // Act
        var response = await StubHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        stub.Calls.Should().Be(1);
    }

    [TestCaseSource(nameof(Lambdas))]
    public async Task GetHttpClient_WhenRepeating_ShouldTellTheCallerAboutEachRepeatWithoutTheAddress(string lambda)
    {
        // Arrange
        var time = new FakeTimeProvider();
        var events = new List<RetryEvent>();
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var client = Factory(lambda)(events.Add, null, stub, time);

        // Act
        await StubHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        var retry = events.Should().ContainSingle().Subject;
        retry.Attempt.Should().Be(1);
        retry.Reason.Should().Be("HTTP 503");
        retry.ToString().Should().NotContain("api.example").And.NotContain("secret-token");
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

    [TestCaseSource(nameof(Lambdas))]
    public void GetHttpClient_CalledTwice_ShouldReturnTwoClientsThatCanBeDisposedSeparately(string lambda)
    {
        // Arrange: the Lambda code disposes every client it gets, as it did with the plain HttpClient before
        var factory = Factory(lambda);
        var first = factory(null, null, new StubHandler(HttpStatusCode.OK), TimeProvider.System);
        var second = factory(null, null, new StubHandler(HttpStatusCode.OK), TimeProvider.System);

        // Act
        first.Dispose();
        var act = () => second.GetAsync(Url).GetAwaiter().GetResult();

        // Assert
        act.Should().NotThrow();
        second.Dispose();
    }
}
