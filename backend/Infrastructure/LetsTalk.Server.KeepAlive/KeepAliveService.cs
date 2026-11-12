using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.KeepAlive;

// Free-tier stores are deleted or paused after a quiet period. While the host runs, this service sends
// every enabled store a small signal on that store's own interval. A failing store is logged and never stops
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
        var runnable = new List<(IKeepAliveTarget Target, TimeSpan Interval)>();
        foreach (var target in _targets)
        {
            // A store is pinged only when the configuration lists it
            if (!_settings.Targets.TryGetValue(target.Name, out var settings))
            {
                LogNotListed(_logger, target.Name);
            }
            else if (!settings.Enabled)
            {
                LogDisabled(_logger, target.Name);
            }
            else if (settings.Interval <= TimeSpan.Zero)
            {
                LogInvalidInterval(_logger, target.Name, settings.Interval);
            }
            else
            {
                runnable.Add((target, settings.Interval));
            }
        }

        if (runnable.Count == 0)
        {
            LogNothingToDo(_logger);
            return;
        }

        foreach (var (target, interval) in runnable)
        {
            LogScheduled(_logger, target.Name, interval);
        }

        try
        {
            // One wait for all stores, so they can connect first
            await Task.Delay(_settings.StartupDelay, _timeProvider, stoppingToken);

            // Each store has its own loop, so a failing or hung store never delays another
            await Task.WhenAll(runnable.Select(item => RunTargetAsync(item.Target, item.Interval, stoppingToken)));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is shutting down
        }
    }

    private async Task RunTargetAsync(IKeepAliveTarget target, TimeSpan interval, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval, _timeProvider);
        do
        {
            await KeepAliveAsync(target, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task KeepAliveAsync(IKeepAliveTarget target, CancellationToken stoppingToken)
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Keep-alive has nothing to do: no store is enabled with a valid interval")]
    private static partial void LogNothingToDo(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keep-alive for {Target} is switched off")]
    private static partial void LogDisabled(ILogger logger, string target);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keep-alive for {Target} is not listed in the settings, so it is not signalled")]
    private static partial void LogNotListed(ILogger logger, string target);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keep-alive for {Target} every {Interval}")]
    private static partial void LogScheduled(ILogger logger, string target, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Keep-alive for {Target} is skipped: the interval {Interval} is not positive")]
    private static partial void LogInvalidInterval(ILogger logger, string target, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keep-alive signal sent to {Target}")]
    private static partial void LogSucceeded(ILogger logger, string target);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Keep-alive signal to {Target} failed: {Reason}")]
    private static partial void LogFailed(ILogger logger, Exception exception, string target, string reason);
}
