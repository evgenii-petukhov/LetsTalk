using LetsTalk.Server.Configuration.Models;

namespace LetsTalk.Server.Resilience;

/// <summary>
/// Builds the HTTP client of a caller without dependency injection (the Lambdas), so the two Lambdas cannot drift apart
/// (spec 006, FR-008, plan D12). The client repeats a failed call by the rules of <see cref="HttpRetryPipeline"/>.
/// </summary>
public static class RetryingHttpClient
{
    /// <param name="onRetry">Told about each repeat.</param>
    /// <param name="settings">The numbers; the defaults when null, because a Lambda reads no configuration.</param>
    /// <param name="innerHandler">Sends the request; a new <see cref="HttpClientHandler"/> when null. A test replaces the network here.</param>
    /// <param name="timeProvider">The clock of the pauses and time limits; the system clock when null. A test replaces it here.</param>
    public static HttpClient Create(
        Action<RetryEvent>? onRetry = null,
        HttpRetrySettings? settings = null,
        HttpMessageHandler? innerHandler = null,
        TimeProvider? timeProvider = null)
    {
        return new HttpClient(new RetryingHttpHandler(
            innerHandler ?? new HttpClientHandler(),
            settings ?? new HttpRetrySettings(),
            onRetry,
            timeProvider));
    }
}
