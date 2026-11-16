using LetsTalk.Server.LinkPreview.Utility.Abstractions;
using LetsTalk.Server.Resilience;

namespace LetsTalk.Server.LinkPreview.Lambda;

/// <summary>
/// The Lambda has no dependency injection, so it builds its clients here. Each client repeats a failed call by the same
/// rules as the services (spec 006) and, because the Lambda reads no configuration, with the default numbers.
/// The parameters let a test replace the network, the numbers and the clock; the Lambda passes only <paramref name="onRetry"/>.
/// </summary>
public class FakeHttpClientService(
    Action<RetryEvent>? onRetry = null,
    HttpRetrySettings? settings = null,
    HttpMessageHandler? innerHandler = null,
    TimeProvider? timeProvider = null) : IHttpClientService
{
    public HttpClient GetHttpClient()
    {
        return new HttpClient(new RetryingHttpHandler(
            innerHandler ?? new HttpClientHandler(),
            settings ?? new HttpRetrySettings(),
            onRetry,
            timeProvider));
    }
}
