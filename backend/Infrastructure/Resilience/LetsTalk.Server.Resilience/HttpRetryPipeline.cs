using System.Net;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace LetsTalk.Server.Resilience;

/// <summary>
/// The one definition of how an HTTP call is repeated (spec 006, FR-001 to FR-004). The services attach it with
/// <c>AddResilienceHandler</c>, the Lambdas through <see cref="RetryingHttpHandler"/>.
/// There is deliberately no circuit breaker and no rate limiter (FR-006).
/// </summary>
public static class HttpRetryPipeline
{
    public static ResiliencePipelineBuilder<HttpResponseMessage> Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        HttpRetrySettings settings,
        Action<RetryEvent>? onRetry = null,
        bool useJitter = true)
    {
        // First added = outermost: the total limit, then the repeats, then the limit of one attempt
        builder.AddTimeout(settings.TotalTimeout);

        if (settings.MaxRetryAttempts > 0)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = settings.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = useJitter,
                Delay = settings.Delay,
                MaxDelay = settings.MaxDelay,
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome.Result, args.Outcome.Exception)),
                DelayGenerator = args => ValueTask.FromResult(RetryAfter(args.Outcome.Result, settings.MaxDelay)),
                OnRetry = args =>
                {
                    // The answer of a failed attempt is not returned to anybody
                    args.Outcome.Result?.Dispose();
                    onRetry?.Invoke(new RetryEvent(args.AttemptNumber + 1, Reason(args.Outcome.Result, args.Outcome.Exception), args.RetryDelay));
                    return default;
                }
            });
        }

        builder.AddTimeout(settings.AttemptTimeout);
        return builder;
    }

    public static ResiliencePipeline<HttpResponseMessage> Create(
        HttpRetrySettings settings,
        Action<RetryEvent>? onRetry = null,
        TimeProvider? timeProvider = null,
        bool useJitter = true)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        if (timeProvider is not null)
        {
            builder.TimeProvider = timeProvider;
        }

        return Configure(builder, settings, onRetry, useJitter).Build();
    }

    /// <summary>
    /// A lost or refused connection, an attempt that took too long, or one of the answers "request timeout",
    /// "too many requests", "bad gateway", "service unavailable" and "gateway timeout". Nothing else (FR-001).
    /// </summary>
    public static bool ShouldRetry(HttpResponseMessage? response, Exception? exception)
    {
        if (exception is not null)
        {
            return exception is HttpRequestException or TimeoutRejectedException;
        }

        return response?.StatusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan? RetryAfter(HttpResponseMessage? response, TimeSpan maxDelay)
    {
        var header = response?.Headers.RetryAfter;
        var wait = header?.Delta ?? (header?.Date is { } date ? date - DateTimeOffset.UtcNow : null);
        return wait is { } value && value > TimeSpan.Zero ? (value < maxDelay ? value : maxDelay) : null;
    }

    private static string Reason(HttpResponseMessage? response, Exception? exception) =>
        exception is not null ? exception.GetType().Name : $"HTTP {(int?)response?.StatusCode}";
}
