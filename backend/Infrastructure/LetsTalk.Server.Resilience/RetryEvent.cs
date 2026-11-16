using System.Globalization;

namespace LetsTalk.Server.Resilience;

/// <summary>
/// What a repeat tells the log: the number of the repeat (1 for the first), the reason, and the pause before it.
/// The reason is the exception type or <c>HTTP 503</c>, never an address, a header or a message text.
/// </summary>
public readonly record struct RetryEvent(int Attempt, string Reason, TimeSpan Delay)
{
    /// <summary>The line for a log that has no message templates (the Lambdas); it names the client, not the address.</summary>
    public string Describe(string target) => string.Create(CultureInfo.InvariantCulture,
        $"Retry {target}: attempt {Attempt} after {Reason}, next try in {Delay.TotalSeconds} s");
}
