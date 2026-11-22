using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Resilience.Tests;

[TestFixture]
public class RetryingHttpHandlerTests
{
    [Test]
    public async Task SendAsync_WhenCreatedWithTheDefaultSettings_ShouldRepeatATransientAnswer()
    {
        // Arrange: the Lambdas build the handler exactly like this, with the defaults and no configuration
        var time = new FakeTimeProvider();
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable).Then(HttpStatusCode.OK);
        using var client = new HttpClient(new RetryingHttpHandler(stub, new HttpRetrySettings(), timeProvider: time), true);

        // Act
        var response = await StubHttpHandler.RunAsync(client.GetAsync("https://page.example/"), time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
    }

    [Test]
    public async Task SendAsync_WhenDisposed_ShouldDisposeTheInnerHandler()
    {
        // Arrange
        var inner = new DisposeProbeHandler();
        var handler = new RetryingHttpHandler(inner, new HttpRetrySettings());

        // Act
        using (new HttpClient(handler, true))
        {
            await Task.CompletedTask;
        }

        // Assert
        inner.Disposed.Should().BeTrue();
    }

    private sealed class DisposeProbeHandler : HttpMessageHandler
    {
        public bool Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
