using Polly;

namespace LetsTalk.Server.Resilience;

/// <summary>
/// Repeats a failed call by the rules of <see cref="HttpRetryPipeline"/>. For callers without dependency injection
/// (the Lambdas); the services use <c>AddResilienceHandler</c> with the same pipeline.
/// </summary>
public class RetryingHttpHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public RetryingHttpHandler(
        HttpMessageHandler innerHandler,
        HttpRetrySettings settings,
        Action<RetryEvent>? onRetry = null,
        TimeProvider? timeProvider = null,
        bool useJitter = true) : base(innerHandler)
    {
        _pipeline = HttpRetryPipeline.Create(settings, onRetry, timeProvider, useJitter);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(
            async token => await base.SendAsync(request, token).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }
}
