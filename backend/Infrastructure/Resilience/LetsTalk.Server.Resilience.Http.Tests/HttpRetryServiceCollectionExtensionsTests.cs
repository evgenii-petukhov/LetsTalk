using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Resilience.Http.Tests;

[TestFixture]
public class HttpRetryServiceCollectionExtensionsTests
{
    private const string Target = "TestTarget";
    private const string Url = "https://api.example/reports?token=secret-token";

    // The shortest pauses the validator allows, so the tests do not wait
    private static readonly Dictionary<string, string?> FastRetries = new()
    {
        ["Resilience:Http:Delay"] = "00:00:00.100",
        ["Resilience:Http:MaxDelay"] = "00:00:00.100"
    };

    private static ServiceProvider Build(
        StubHandler stub,
        Dictionary<string, string?>? settings = null,
        List<string>? log = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? FastRetries).Build();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(new ListLoggerProvider(log ?? [])));
        services.AddHttpRetrySettings(configuration);
        services.AddHttpClient(Target).AddHttpRetry(Target).ConfigurePrimaryHttpMessageHandler(() => stub);
        return services.BuildServiceProvider();
    }

    private static HttpClient Client(ServiceProvider provider) =>
        provider.GetRequiredService<IHttpClientFactory>().CreateClient(Target);

    [Test]
    public async Task Send_WhenFirstAnswerIsTransient_ShouldRepeatAndReturnTheLaterAnswer()
    {
        // Arrange
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var provider = Build(stub);

        // Act
        var response = await Client(provider).PostAsync(Url, new StringContent("{}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.Calls.Should().Be(2);
    }

    [Test]
    public async Task Send_WhenAnswerIsNotTransient_ShouldNotRepeat()
    {
        // Arrange
        var stub = new StubHandler(HttpStatusCode.NotFound, HttpStatusCode.OK);
        using var provider = Build(stub);

        // Act
        var response = await Client(provider).GetAsync(Url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        stub.Calls.Should().Be(1);
    }

    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    public async Task Send_WhenMaxRetryAttemptsIsConfigured_ShouldUseIt(int retries, int expectedCalls)
    {
        // Arrange
        var settings = new Dictionary<string, string?>(FastRetries) { ["Resilience:Http:MaxRetryAttempts"] = retries.ToString() };
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable);
        using var provider = Build(stub, settings);

        // Act
        var response = await Client(provider).GetAsync(Url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        stub.Calls.Should().Be(expectedCalls);
    }

    [Test]
    public async Task Send_WhenMaxRetryAttemptsIsNotConfigured_ShouldUseTheDefault()
    {
        // Arrange: only the pauses are shortened
        var stub = new StubHandler(HttpStatusCode.BadGateway);
        using var provider = Build(stub);

        // Act
        await Client(provider).GetAsync(Url);

        // Assert
        stub.Calls.Should().Be(new HttpRetrySettings().MaxRetryAttempts + 1);
    }

    [Test]
    public async Task Send_WhenRepeating_ShouldLogAWarningWithTargetAttemptAndReasonButNotTheAddress()
    {
        // Arrange
        var log = new List<string>();
        var stub = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var provider = Build(stub, log: log);

        // Act
        await Client(provider).PostAsync(Url, new StringContent("{\"title\":\"private text\"}"));

        // Assert: the library's own telemetry adds two more warnings per repeat; none may show the address or any text
        var line = log.Should().ContainSingle(x => x.Contains("Retry TestTarget")).Subject;
        line.Should().StartWith("Warning:").And.Contain("attempt 1").And.Contain("HTTP 503");
        log.Should().OnlyContain(x => !x.Contains("api.example") && !x.Contains("secret-token") && !x.Contains("private text"));
    }

    [Test]
    public void Options_WhenSettingsAreWrong_ShouldFailAndNameTheKey()
    {
        // Arrange
        var settings = new Dictionary<string, string?> { ["Resilience:Http:Delay"] = "00:00:00" };
        using var provider = Build(new StubHandler(HttpStatusCode.OK), settings);

        // Act
        var act = () => provider.GetRequiredService<IOptions<HttpRetrySettings>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*Resilience:Http:Delay*");
    }

    [Test]
    public async Task Send_WhenTwoClientsAreRegistered_ShouldKeepTheirRepeatsApart()
    {
        // Arrange: one failing client does not slow or change the other (spec FR-006)
        var failing = new StubHandler(HttpStatusCode.ServiceUnavailable);
        var healthy = new StubHandler(HttpStatusCode.OK);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(FastRetries).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpRetrySettings(configuration);
        services.AddHttpClient("failing").AddHttpRetry("failing").ConfigurePrimaryHttpMessageHandler(() => failing);
        services.AddHttpClient("healthy").AddHttpRetry("healthy").ConfigurePrimaryHttpMessageHandler(() => healthy);
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act
        await factory.CreateClient("failing").GetAsync(Url);
        var response = await factory.CreateClient("healthy").GetAsync(Url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        healthy.Calls.Should().Be(1);
        failing.Calls.Should().Be(4);
    }

    private sealed class StubHandler(params HttpStatusCode[] answers) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var status = answers[Math.Min(Calls, answers.Length - 1)];
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class ListLoggerProvider(List<string> lines) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(lines);

        public void Dispose()
        {
        }

        private sealed class ListLogger(List<string> lines) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel >= LogLevel.Warning)
                {
                    lines.Add($"{logLevel}: {formatter(state, exception)}");
                }
            }
        }
    }
}
