using Microsoft.Extensions.Logging;

namespace LetsTalk.Server.Resilience;

internal static partial class HttpRetryLog
{
    // The message names the client, not the address, and holds no header or body (spec 006, FR-010)
    [LoggerMessage(Level = LogLevel.Warning, Message = "Retry {Target}: attempt {Attempt} after {Reason}, next try in {DelaySeconds} s")]
    public static partial void Retrying(ILogger logger, string target, int attempt, string reason, double delaySeconds);
}
