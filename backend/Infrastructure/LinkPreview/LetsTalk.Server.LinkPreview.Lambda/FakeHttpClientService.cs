using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.LinkPreview.Utility.Abstractions;
using LetsTalk.Server.Resilience;

namespace LetsTalk.Server.LinkPreview.Lambda;

/// <summary>
/// The Lambda has no dependency injection, so it builds its client here, with <see cref="RetryingHttpClient"/>
/// (the rules of the services, spec 006, and the default numbers, because the Lambda reads no configuration).
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
        return RetryingHttpClient.Create(onRetry, settings, innerHandler, timeProvider);
    }
}
