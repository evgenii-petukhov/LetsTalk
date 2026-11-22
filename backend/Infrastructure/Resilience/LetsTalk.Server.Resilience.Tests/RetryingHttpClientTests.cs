using System.Net;
using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Time.Testing;

namespace LetsTalk.Server.Resilience.Tests;

// The Lambdas build their client with this factory, so these are the retry rules as the Lambdas get them (spec 006, plan D12).
// Every case passes an inner stub: with no inner handler the factory would use the real network.
[TestFixture]
public class RetryingHttpClientTests
{
    private const string Url = "https://api.example/reports?token=secret-token";

    [Test]
    public async Task Create_WhenTheAnswerIsTransient_ShouldRepeatItTheDefaultNumberOfTimes()
    {
        // Arrange: no settings are given, as in the deployed Lambda
        var time = new FakeTimeProvider();
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable);
        using var client = RetryingHttpClient.Create(innerHandler: stub, timeProvider: time);

        // Act
        var response = await StubHttpHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        stub.Calls.Should().Be(new HttpRetrySettings().MaxRetryAttempts + 1);
    }

    [Test]
    public async Task Create_WhenSettingsAreGiven_ShouldUseThem()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable);
        using var client = RetryingHttpClient.Create(settings: new HttpRetrySettings { MaxRetryAttempts = 1 }, innerHandler: stub, timeProvider: time);

        // Act
        await StubHttpHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        stub.Calls.Should().Be(2);
    }

    [Test]
    public async Task Create_WhenFirstAnswerIsTransient_ShouldReturnTheLaterAnswer()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var stub = new StubHttpHandler().Then(HttpStatusCode.BadGateway).Then(HttpStatusCode.OK);
        using var client = RetryingHttpClient.Create(innerHandler: stub, timeProvider: time);

        // Act
        var response = await StubHttpHandler.RunAsync(client.PatchAsync(Url, new StringContent("{}")), time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
        stub.Bodies.Should().Equal("{}", "{}");
    }

    [Test]
    public async Task Create_WhenAnswerIsNotTransient_ShouldNotRepeat()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var stub = new StubHttpHandler().Then(HttpStatusCode.NotFound).Then(HttpStatusCode.OK);
        using var client = RetryingHttpClient.Create(innerHandler: stub, timeProvider: time);

        // Act
        var response = await StubHttpHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        stub.Calls.Should().Be(1);
    }

    [Test]
    public async Task Create_WhenRepeating_ShouldCallOnRetryWithoutTheAddress()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var events = new List<RetryEvent>();
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable).Then(HttpStatusCode.OK);
        using var client = RetryingHttpClient.Create(events.Add, innerHandler: stub, timeProvider: time);

        // Act
        await StubHttpHandler.RunAsync(client.GetAsync(Url), time);

        // Assert
        var retry = events.Should().ContainSingle().Subject;
        retry.Attempt.Should().Be(1);
        retry.Reason.Should().Be("HTTP 503");
        retry.ToString().Should().NotContain("api.example").And.NotContain("secret-token");
    }

    [Test]
    public void Create_CalledTwice_ShouldReturnClientsThatCanBeDisposedSeparately()
    {
        // Arrange: the Lambda code disposes every client it gets, as it did with the plain HttpClient before
        var first = RetryingHttpClient.Create(innerHandler: new StubHttpHandler().Then(HttpStatusCode.OK));
        var second = RetryingHttpClient.Create(innerHandler: new StubHttpHandler().Then(HttpStatusCode.OK));

        // Act
        first.Dispose();
        var act = () => second.GetAsync(Url).GetAwaiter().GetResult();

        // Assert
        act.Should().NotThrow();
        second.Dispose();
    }
}
