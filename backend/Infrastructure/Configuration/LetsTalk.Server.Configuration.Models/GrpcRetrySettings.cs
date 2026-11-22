namespace LetsTalk.Server.Configuration.Models;

/// <summary>How a read-only gRPC call is repeated when the service cannot be reached (spec 006).</summary>
public class GrpcRetrySettings
{
    /// <summary>All attempts including the first, 1 to 5 (the gRPC limit); 1 = no repeat.</summary>
    public int MaxAttempts { get; set; } = 4;

    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(10);
}
