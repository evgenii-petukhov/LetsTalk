using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Polly.Timeout;
using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Resilience.Tests;

[TestFixture]
public class HttpRetryPipelineTests
{
    private const string Url = "https://api.example/reports";
    private const string Body = "{\"messageId\":\"m1\"}";
    private const string Token = "Bearer secret-token";

    private FakeTimeProvider _time = null!;
    private List<RetryEvent> _events = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider();
        _events = [];
    }

    private static HttpRetrySettings Settings(int retries = 3) => new()
    {
        MaxRetryAttempts = retries,
        Delay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(10),
        AttemptTimeout = TimeSpan.FromSeconds(30),
        TotalTimeout = TimeSpan.FromSeconds(100)
    };

    private HttpClient Client(StubHttpHandler stub, HttpRetrySettings settings, bool jitter = false) =>
        new(new RetryingHttpHandler(stub, settings, _events.Add, _time, jitter), true);

    private static HttpRequestMessage Post() => new(HttpMethod.Post, Url)
    {
        Content = new StringContent(Body),
        Headers = { Authorization = AuthenticationHeaderValue.Parse(Token) }
    };

    [TestCase(HttpStatusCode.RequestTimeout)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    [TestCase(HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    [TestCase(HttpStatusCode.GatewayTimeout)]
    public async Task Send_WhenFirstAnswerIsTransient_ShouldRepeatAndReturnTheLaterAnswer(HttpStatusCode transient)
    {
        // Arrange
        var stub = new StubHttpHandler().Then(transient).Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        var response = await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
    }

    [Test]
    public async Task Send_WhenConnectionFails_ShouldRepeat()
    {
        // Arrange
        var stub = new StubHttpHandler().ThenThrow(new HttpRequestException("connection refused")).Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        var response = await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
    }

    [Test]
    public async Task Send_WhenAttemptTimesOut_ShouldRepeat()
    {
        // Arrange
        var stub = new StubHttpHandler().ThenHang().Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        var response = await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
        _events.Should().ContainSingle().Which.Reason.Should().Contain("Timeout");
    }

    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task Send_WhenAnswerIsNotTransient_ShouldNotRepeat(HttpStatusCode status)
    {
        // Arrange
        var stub = new StubHttpHandler().Then(status).Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        var response = await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        response.StatusCode.Should().Be(status);
        stub.Calls.Should().Be(1);
        _events.Should().BeEmpty();
    }

    [Test]
    public async Task Send_WhenAllAttemptsFail_ShouldReturnTheLastAnswerAfterTheConfiguredRepeats()
    {
        // Arrange
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable);
        using var client = Client(stub, Settings(retries: 3));

        // Act
        var response = await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        stub.Calls.Should().Be(4);
        _events.Select(x => x.Attempt).Should().Equal(1, 2, 3);
    }

    [Test]
    public async Task Send_WhenRetriesAreZero_ShouldTryOnce()
    {
        // Arrange
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable);
        using var client = Client(stub, Settings(retries: 0));

        // Act
        var response = await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        stub.Calls.Should().Be(1);
    }

    [Test]
    public async Task Send_WhenRepeating_ShouldSendTheSameBodyAndHeaders()
    {
        // Arrange
        var stub = new StubHttpHandler().Then(HttpStatusCode.BadGateway).Then(HttpStatusCode.BadGateway).Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        stub.Bodies.Should().Equal(Body, Body, Body);
        stub.Authorizations.Should().Equal(Token, Token, Token);
    }

    [Test]
    public async Task Send_WhenAnswerHasRetryAfter_ShouldWaitThatLong()
    {
        // Arrange
        var stub = new StubHttpHandler()
            .Then(HttpStatusCode.TooManyRequests, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7)))
            .Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        _events.Should().ContainSingle().Which.Delay.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Test]
    public async Task Send_WhenRetryAfterIsLongerThanMaxDelay_ShouldWaitMaxDelay()
    {
        // Arrange
        var stub = new StubHttpHandler()
            .Then(HttpStatusCode.ServiceUnavailable, r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(5)))
            .Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        _events.Should().ContainSingle().Which.Delay.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Send_WhenPausing_ShouldGrowTheDelayWithEachRepeatUpToMaxDelay()
    {
        // Arrange
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable);
        var settings = Settings(retries: 5);
        using var client = Client(stub, settings, jitter: false);

        // Act
        await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        _events.Select(x => x.Delay.TotalSeconds).Should().Equal(1, 2, 4, 8, 10);
    }

    [Test]
    public async Task Send_WhenJitterIsOn_ShouldKeepEveryDelayBetweenZeroAndMaxDelay()
    {
        // Arrange
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable);
        using var client = Client(stub, Settings(retries: 5), jitter: true);

        // Act
        await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        _events.Should().HaveCount(5).And.OnlyContain(x => x.Delay > TimeSpan.Zero && x.Delay <= TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Send_WhenCancelled_ShouldNotRepeatAndShouldStop()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var stub = new StubHttpHandler().ThenHang();
        using var client = Client(stub, Settings());

        // Act
        var task = client.SendAsync(Post(), cts.Token);
        await cts.CancelAsync();
        var act = async () => await task;

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        stub.Calls.Should().Be(1);
        _events.Should().BeEmpty();
    }

    [Test]
    public async Task Send_WhenTotalTimeoutPasses_ShouldStopRepeating()
    {
        // Arrange
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable);
        var settings = Settings(retries: 10);
        settings.Delay = TimeSpan.FromSeconds(4);
        settings.TotalTimeout = TimeSpan.FromSeconds(10);
        using var client = Client(stub, settings);

        // Act
        var act = async () => await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        await act.Should().ThrowAsync<TimeoutRejectedException>();
        stub.Calls.Should().BeLessThan(11);
    }

    [Test]
    public async Task Send_WhenRepeating_ShouldCallOnRetryWithTheTargetFreeReasonOnly()
    {
        // Arrange
        var stub = new StubHttpHandler().Then(HttpStatusCode.ServiceUnavailable).Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        var retry = _events.Should().ContainSingle().Subject;
        retry.Attempt.Should().Be(1);
        retry.Reason.Should().Be("HTTP 503");
        retry.ToString().Should().NotContain("api.example").And.NotContain("secret-token").And.NotContain("m1");
    }

    [Test]
    public async Task Send_WhenConnectionFailsWithAMessageThatHoldsTheAddress_ShouldNotPutTheMessageInTheReason()
    {
        // Arrange
        var stub = new StubHttpHandler()
            .ThenThrow(new HttpRequestException("No connection could be made to https://api.example/reports?token=secret-token"))
            .Then(HttpStatusCode.OK);
        using var client = Client(stub, Settings());

        // Act
        await StubHttpHandler.RunAsync(client.SendAsync(Post()), _time);

        // Assert
        _events.Should().ContainSingle().Which.Reason.Should().Be(nameof(HttpRequestException));
    }
}
