using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.KeepAlive;

// Free-tier stores are deleted or paused after a quiet period. While the host runs, this service sends
// every registered store a small signal at a fixed interval. A failing store is logged and never stops
// the schedule, the other stores or the host.
public partial class KeepAliveService(
    IEnumerable<IKeepAliveTarget> targets,
    IOptions<KeepAliveSettings> options,
    TimeProvider timeProvider,
    ILogger<KeepAliveService> logger) : BackgroundService
{
    // One signal is a single round trip, so anything slower is a hung connection.
    public static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(30);

    private readonly IReadOnlyList<IKeepAliveTarget> _targets = [.. targets];
    private readonly KeepAliveSettings _settings = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<KeepAliveService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled || _targets.Count == 0)
        {
            LogNothingToDo(_logger, _settings.Enabled, _targets.Count);
            return;
        }

        try
        {
            await Task.Delay(_settings.StartupDelay, _timeProvider, stoppingToken);

            using var timer = new PeriodicTimer(_settings.Interval, _timeProvider);
            do
            {
                await KeepAliveAllAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is shutting down
        }
    }

    private async Task KeepAliveAllAsync(CancellationToken stoppingToken)
    {
        foreach (var target in _targets)
        {
            using var timeout = new CancellationTokenSource(PingTimeout, _timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);

            try
            {
                await target.KeepAliveAsync(linked.Token);
                LogSucceeded(_logger, target.Name);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogFailed(_logger, ex, target.Name, ex.Message);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Keep-alive does nothing: enabled is {Enabled}, {TargetCount} store(s) are configured")]
    private static partial void LogNothingToDo(ILogger logger, bool enabled, int targetCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Keep-alive signal sent to {Target}")]
    private static partial void LogSucceeded(ILogger logger, string target);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Keep-alive signal to {Target} failed: {Reason}")]
    private static partial void LogFailed(ILogger logger, Exception exception, string target, string reason);
}
