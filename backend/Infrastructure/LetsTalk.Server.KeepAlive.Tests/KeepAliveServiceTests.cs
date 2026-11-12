using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LetsTalk.Server.KeepAlive.Tests;

[TestFixture]
public class KeepAliveServiceTests
{
    private const string FirstTargetName = "First";
    private const string SecondTargetName = "Second";
    private const string UnknownTargetName = "Typo";
    private const string SignalSentText = "signal sent";
    private const string NotListedText = "not listed";
    private const string NothingToDoText = "nothing to do";
    private const string FailureMessage = "the store is unreachable";

    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);

    private FakeTimeProvider _time;
    private ListLogger _logger;
    private KeepAliveSettings _settings;
    private TestableKeepAliveService? _service;
    private Task? _run;
    private CancellationTokenSource _stopping;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider();
        _stopping = new CancellationTokenSource();
        _service = null;
        _run = null;
        _logger = new ListLogger();
        _settings = new KeepAliveSettings { StartupDelay = StartupDelay };
    }

    [TearDown]
    public async Task TearDown()
    {
        await _stopping.CancelAsync();
        if (_run != null)
        {
            await _run.WaitAsync(WaitLimit);
        }

        _service?.Dispose();
        _stopping.Dispose();
    }

    [Test]
    public async Task ExecuteAsync_WhenStarted_ShouldPingAllTargetsAfterTheStartupDelay()
    {
        // Arrange
        var first = new RecordingTarget(FirstTargetName);
        var second = new RecordingTarget(SecondTargetName);
        Start(first, second);

        // Act, nothing happens before the delay
        _time.Advance(StartupDelay - TimeSpan.FromSeconds(1));
        await Task.Yield();
        first.Calls.Should().Be(0);
        _time.Advance(TimeSpan.FromSeconds(1));
        await first.WaitForCallsAsync(1);
        await second.WaitForCallsAsync(1);

        // Assert
        first.Calls.Should().Be(1);
        second.Calls.Should().Be(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenIntervalPasses_ShouldPingAgain()
    {
        // Arrange
        var target = new RecordingTarget(FirstTargetName);
        Start(target);
        _time.Advance(StartupDelay);
        await target.WaitForCallsAsync(1);

        // Act
        _time.Advance(Interval);
        await target.WaitForCallsAsync(2);
        _time.Advance(Interval);
        await target.WaitForCallsAsync(3);

        // Assert
        target.Calls.Should().Be(3);
    }

    [Test]
    public async Task ExecuteAsync_WhenOneTargetFails_ShouldStillPingTheOthers()
    {
        // Arrange
        var failing = new RecordingTarget(FirstTargetName) { Failure = new InvalidOperationException(FailureMessage) };
        var healthy = new RecordingTarget(SecondTargetName);
        Start(failing, healthy);

        // Act
        _time.Advance(StartupDelay);
        await healthy.WaitForCallsAsync(1);

        // Assert
        failing.Calls.Should().Be(1);
        healthy.Calls.Should().Be(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetFails_ShouldLogAWarningWithTheNameAndKeepRunning()
    {
        // Arrange
        var failing = new RecordingTarget(FirstTargetName) { Failure = new InvalidOperationException(FailureMessage) };
        Start(failing);
        _time.Advance(StartupDelay);
        await failing.WaitForCallsAsync(1);

        // Act, the schedule continues after the failure
        _time.Advance(Interval);
        await failing.WaitForCallsAsync(2);

        // Assert
        var warnings = _logger.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();
        warnings.Should().HaveCountGreaterThanOrEqualTo(1);
        warnings[0].Message.Should().Contain(FirstTargetName).And.Contain(FailureMessage);
        _run!.IsCompleted.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetSucceeds_ShouldLogOneInformationLine()
    {
        // Arrange
        var target = new RecordingTarget(FirstTargetName);
        Start(target);

        // Act
        _time.Advance(StartupDelay);
        await target.WaitForCallsAsync(1);
        await _logger.WaitForEntryAsync(entry => entry.Message.Contains(SignalSentText));

        // Assert
        _logger.Entries.Where(entry => entry.Message.Contains(SignalSentText))
            .Should().ContainSingle().Which.Level.Should().Be(LogLevel.Information);
        _logger.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetHangs_ShouldCancelThePingAfterTheTimeout()
    {
        // Arrange
        var hanging = new RecordingTarget(FirstTargetName) { Hang = true };
        var healthy = new RecordingTarget(SecondTargetName);
        Start(hanging, healthy);
        _time.Advance(StartupDelay);
        await hanging.WaitForCallsAsync(1);
        await healthy.WaitForCallsAsync(1);

        // Act
        _time.Advance(KeepAliveService.PingTimeout);
        await _logger.WaitForEntryAsync(entry => entry.Level == LogLevel.Warning);

        // Assert
        hanging.WasCancelled.Should().BeTrue();
        _logger.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains(FirstTargetName));
        _run!.IsCompleted.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetsHaveDifferentIntervals_ShouldPingEachOnItsOwnSchedule()
    {
        // Arrange
        var twiceADay = TimeSpan.FromHours(12);
        SetTarget(FirstTargetName, interval: twiceADay);
        SetTarget(SecondTargetName, interval: Interval);
        var first = new RecordingTarget(FirstTargetName);
        var second = new RecordingTarget(SecondTargetName);
        Start(first, second);
        _time.Advance(StartupDelay);
        await first.WaitForCallsAsync(1);
        await second.WaitForCallsAsync(1);

        // Act
        _time.Advance(twiceADay);
        await first.WaitForCallsAsync(2);

        // Assert, the second store has not been pinged again yet
        second.Calls.Should().Be(1);
        _time.Advance(twiceADay);
        await first.WaitForCallsAsync(3);
        await second.WaitForCallsAsync(2);
    }

    [Test]
    public async Task ExecuteAsync_WhenOneTargetHangs_ShouldNotDelayTheOther()
    {
        // Arrange
        SetTarget(SecondTargetName, interval: TimeSpan.FromSeconds(10));
        var hanging = new RecordingTarget(FirstTargetName) { Hang = true };
        var healthy = new RecordingTarget(SecondTargetName);
        Start(hanging, healthy);
        _time.Advance(StartupDelay);
        await hanging.WaitForCallsAsync(1);
        await healthy.WaitForCallsAsync(1);

        // Act, the first store is still hanging
        _time.Advance(TimeSpan.FromSeconds(10));
        await healthy.WaitForCallsAsync(2);

        // Assert
        hanging.WasCancelled.Should().BeFalse();
        hanging.Calls.Should().Be(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenOneTargetDisabled_ShouldPingOnlyTheOther()
    {
        // Arrange
        SetTarget(FirstTargetName, enabled: false);
        var disabled = new RecordingTarget(FirstTargetName);
        var enabled = new RecordingTarget(SecondTargetName);
        Start(disabled, enabled);

        // Act
        _time.Advance(StartupDelay);
        await enabled.WaitForCallsAsync(1);
        _time.Advance(Interval);
        await enabled.WaitForCallsAsync(2);

        // Assert
        disabled.Calls.Should().Be(0);
    }

    [Test]
    public async Task ExecuteAsync_WhenAllTargetsDisabled_ShouldLogOnceAndNotPing()
    {
        // Arrange
        SetTarget(FirstTargetName, enabled: false);
        SetTarget(SecondTargetName, enabled: false);
        var first = new RecordingTarget(FirstTargetName);
        var second = new RecordingTarget(SecondTargetName);
        Start(first, second);

        // Act
        _time.Advance(StartupDelay + Interval);
        await _run!.WaitAsync(WaitLimit);

        // Assert
        first.Calls.Should().Be(0);
        second.Calls.Should().Be(0);
        _logger.Entries.Should().ContainSingle(entry => entry.Message.Contains(NothingToDoText));
        _logger.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
    }

    [Test]
    public async Task ExecuteAsync_WhenTargetNotListed_ShouldNotPingAndSayWhy()
    {
        // Arrange, only the second store is listed
        SetTarget(SecondTargetName, interval: TimeSpan.FromHours(1));
        var notListed = new RecordingTarget(FirstTargetName);
        var listed = new RecordingTarget(SecondTargetName);
        StartAsIs(notListed, listed);

        // Act
        _time.Advance(StartupDelay);
        await listed.WaitForCallsAsync(1);
        _time.Advance(Interval);
        await listed.WaitForCallsAsync(2);

        // Assert
        notListed.Calls.Should().Be(0);
        _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Information
            && entry.Message.Contains(FirstTargetName) && entry.Message.Contains(NotListedText));
    }

    [Test]
    public async Task ExecuteAsync_WhenNoTargetListed_ShouldLogNothingToDoAndNotPing()
    {
        // Arrange
        var first = new RecordingTarget(FirstTargetName);
        var second = new RecordingTarget(SecondTargetName);
        StartAsIs(first, second);

        // Act
        _time.Advance(StartupDelay + Interval);
        await _run!.WaitAsync(WaitLimit);

        // Assert
        first.Calls.Should().Be(0);
        second.Calls.Should().Be(0);
        _logger.Entries.Should().ContainSingle(entry => entry.Message.Contains(NothingToDoText));
        _logger.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task ExecuteAsync_WhenIntervalNotPositive_ShouldSkipThatTargetAndWarn(int seconds)
    {
        // Arrange
        SetTarget(FirstTargetName, interval: TimeSpan.FromSeconds(seconds));
        var skipped = new RecordingTarget(FirstTargetName);
        var healthy = new RecordingTarget(SecondTargetName);
        Start(skipped, healthy);

        // Act
        _time.Advance(StartupDelay);
        await healthy.WaitForCallsAsync(1);

        // Assert
        skipped.Calls.Should().Be(0);
        _logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains(FirstTargetName));
        _run!.IsCompleted.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenSettingsNameNoTarget_ShouldIgnoreItWithoutAWarning()
    {
        // Arrange
        SetTarget(UnknownTargetName, interval: Interval);
        var target = new RecordingTarget(FirstTargetName);
        Start(target);

        // Act
        _time.Advance(StartupDelay);
        await target.WaitForCallsAsync(1);

        // Assert
        _logger.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
        _logger.Entries.Should().NotContain(entry => entry.Message.Contains(UnknownTargetName));
    }

    [Test]
    public async Task ExecuteAsync_WhenSettingsNameDiffersOnlyInLetterCase_ShouldNotWarn()
    {
        // Arrange
        SetTarget(FirstTargetName.ToUpperInvariant(), interval: TimeSpan.FromHours(12));
        var target = new RecordingTarget(FirstTargetName);
        Start(target);
        _time.Advance(StartupDelay);
        await target.WaitForCallsAsync(1);

        // Act, the settings found by the other letter case apply
        _time.Advance(TimeSpan.FromHours(12));
        await target.WaitForCallsAsync(2);

        // Assert
        _logger.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
    }

    [Test]
    public void ExecuteAsync_WhenStarted_ShouldLogTheIntervalOfEachRunnableTarget()
    {
        // Arrange
        var twiceADay = TimeSpan.FromHours(12);
        SetTarget(FirstTargetName, interval: twiceADay);
        var first = new RecordingTarget(FirstTargetName);
        var second = new RecordingTarget(SecondTargetName);

        // Act
        Start(first, second);

        // Assert, before the start-up delay has passed
        _logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Information
            && entry.Message.Contains(FirstTargetName) && entry.Message.Contains(twiceADay.ToString()));
        _logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Information
            && entry.Message.Contains(SecondTargetName) && entry.Message.Contains(Interval.ToString()));
        first.Calls.Should().Be(0);
    }

    [Test]
    public async Task ExecuteAsync_WhenNoTargets_ShouldNotPing()
    {
        // Arrange
        Start();

        // Act
        _time.Advance(StartupDelay + Interval);
        await _run!.WaitAsync(WaitLimit);

        // Assert
        _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Information);
    }

    [Test]
    public async Task ExecuteAsync_WhenStopped_ShouldEndQuickly()
    {
        // Arrange
        var target = new RecordingTarget(FirstTargetName);
        Start(target);

        // Act, stopped while still waiting for the startup delay
        await _stopping.CancelAsync();
        await _run!.WaitAsync(WaitLimit);

        // Assert
        _run.IsCompleted.Should().BeTrue();
        target.Calls.Should().Be(0);
    }

    private void SetTarget(string name, bool enabled = true, TimeSpan? interval = null)
    {
        _settings.Targets[name] = new KeepAliveTargetSettings { Enabled = enabled, Interval = interval ?? Interval };
    }

    // Lists every target with the default settings, unless the test has listed it itself
    private void Start(params RecordingTarget[] targets)
    {
        foreach (var target in targets)
        {
            _settings.Targets.TryAdd(target.Name, new KeepAliveTargetSettings());
        }

        StartAsIs(targets);
    }

    // Lists nothing: only the entries the test has added are in the settings
    private void StartAsIs(params RecordingTarget[] targets)
    {
        _service = new TestableKeepAliveService(targets, Options.Create(_settings), _time, _logger);

        // Runs ExecuteAsync up to its first await on this thread, so the startup timer exists
        // before the test moves the clock (BackgroundService.StartAsync starts it asynchronously)
        _run = _service.RunAsync(_stopping.Token);
    }

    private sealed class TestableKeepAliveService(
        IEnumerable<IKeepAliveTarget> targets,
        IOptions<KeepAliveSettings> options,
        TimeProvider timeProvider,
        ILogger<KeepAliveService> logger) : KeepAliveService(targets, options, timeProvider, logger)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }

    private sealed class RecordingTarget(string name) : IKeepAliveTarget
    {
        private readonly SemaphoreSlim _called = new(0);
        private int _calls;

        public string Name { get; } = name;

        public Exception? Failure { get; init; }

        public bool Hang { get; init; }

        public bool WasCancelled { get; private set; }

        public int Calls => Volatile.Read(ref _calls);

        public async Task KeepAliveAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            _called.Release();

            if (Hang)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    WasCancelled = true;
                    throw;
                }
            }

            if (Failure != null)
            {
                throw Failure;
            }
        }

        public async Task WaitForCallsAsync(int expected)
        {
            while (Calls < expected)
            {
                if (!await _called.WaitAsync(WaitLimit))
                {
                    throw new TimeoutException($"{Name} was called {Calls} time(s), expected {expected}.");
                }
            }
        }
    }

    private sealed class ListLogger : ILogger<KeepAliveService>
    {
        private readonly object _lock = new();
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries
        {
            get
            {
                lock (_lock)
                {
                    return [.. _entries];
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            lock (_lock)
            {
                _entries.Add((logLevel, exception == null ? message : $"{message} {exception.Message}"));
            }
        }

        public async Task WaitForEntryAsync(Func<(LogLevel Level, string Message), bool> predicate)
        {
            var deadline = DateTime.UtcNow + WaitLimit;
            while (!Entries.Any(predicate))
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("The expected log entry did not appear.");
                }

                await Task.Delay(10);
            }
        }
    }
}
