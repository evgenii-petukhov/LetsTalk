using FluentAssertions;
using LetsTalk.Server.Configuration.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class AddValidatedSettingsTests
{
    private const string Section = "Sample";
    private const string CountKey = "Sample:Count";
    private const string NameKey = "Sample:Name";

    [Test]
    public void AddValidatedSettings_WhenSectionIsValid_ShouldBindIt()
    {
        // Arrange
        using var provider = Build((CountKey, "7"), (NameKey, "abc"));

        // Act
        var settings = provider.GetRequiredService<IOptions<SampleSettings>>().Value;

        // Assert
        settings.Count.Should().Be(7);
        settings.Name.Should().Be("abc");
    }

    [Test]
    public void AddValidatedSettings_WhenSectionIsMissing_ShouldKeepTheDefaults()
    {
        // Arrange
        using var provider = Build();

        // Act
        var settings = provider.GetRequiredService<IOptions<SampleSettings>>().Value;

        // Assert
        settings.Count.Should().Be(5);
        settings.Name.Should().Be("default");
    }

    [Test]
    public void AddValidatedSettings_WhenSeveralValuesAreWrong_ShouldListEveryFailure()
    {
        // Arrange
        using var provider = Build((CountKey, "0"), (NameKey, " "));

        // Act
        var act = () => provider.GetRequiredService<IOptions<SampleSettings>>().Value;

        // Assert
        var exception = act.Should().Throw<OptionsValidationException>().Which;
        exception.Failures.Should().BeEquivalentTo(
            [$"{CountKey} must be between 1 and 10, but is 0.", $"{NameKey} must be set."]);
    }

    [Test]
    public void AddValidatedSettings_WhenValueHasTheWrongType_ShouldNameTheKey()
    {
        // Arrange
        using var provider = Build((CountKey, "twenty"));

        // Act
        var act = () => provider.GetRequiredService<IOptions<SampleSettings>>().Value;

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{CountKey}*");
    }

    [Test]
    public void AddValidatedSettings_WhenNumberIsEmptyText_ShouldFailAndNameTheKey()
    {
        // Arrange: an environment variable set to nothing (LetsTalk_Sample__Count=) arrives as empty text
        using var provider = Build((CountKey, ""));

        // Act
        var act = () => provider.GetRequiredService<IOptions<SampleSettings>>().Value;

        // Assert: the binder does not keep the default; it refuses, and the message names the key
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{CountKey}*");
    }

    [Test]
    public void AddValidatedSettings_WhenCalledTwiceForOneClass_ShouldReportEachFailureOnce()
    {
        // Arrange
        var configuration = CreateConfiguration((CountKey, "0"));
        var services = new ServiceCollection();
        services.AddValidatedSettings<SampleSettings, SampleSettingsValidator>(configuration, Section);
        services.AddValidatedSettings<SampleSettings, SampleSettingsValidator>(configuration, Section);
        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<SampleSettings>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().ContainSingle();
    }

    [Test]
    public async Task AddValidatedSettings_WhenGenericHostStartsWithAWrongValue_ShouldFailTheStart()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(Pairs((CountKey, "0")));
        builder.Services.AddValidatedSettings<SampleSettings, SampleSettingsValidator>(builder.Configuration, Section);
        using var host = builder.Build();

        // Act
        var act = () => host.StartAsync();

        // Assert
        await act.Should().ThrowAsync<OptionsValidationException>().WithMessage($"*{CountKey}*");
    }

    [Test]
    public async Task AddValidatedSettings_WhenWebHostStartsWithAWrongValue_ShouldFailTheStartBeforeListening()
    {
        // Arrange
        var logs = new LogCollector();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(logs);
        builder.Configuration.AddInMemoryCollection(Pairs((CountKey, "0")));
        builder.Services.AddValidatedSettings<SampleSettings, SampleSettingsValidator>(builder.Configuration, Section);
        await using var app = builder.Build();

        // Act
        var act = () => app.StartAsync();

        // Assert: the check fails the start, and the server never reports that it listens
        await act.Should().ThrowAsync<OptionsValidationException>().WithMessage($"*{CountKey}*");
        logs.Messages.Should().NotContain(x => x.Contains("Now listening on", StringComparison.Ordinal));
    }

    [Test]
    public async Task AddValidatedSettings_WhenWebHostStartsWithValidValues_ShouldStartAndListen()
    {
        // Arrange: the control of the test above, which proves that the log line it looks for does appear
        var logs = new LogCollector();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(logs);
        builder.Configuration.AddInMemoryCollection(Pairs((CountKey, "3")));
        builder.Services.AddValidatedSettings<SampleSettings, SampleSettingsValidator>(builder.Configuration, Section);
        await using var app = builder.Build();

        // Act
        await app.StartAsync();
        await app.StopAsync();

        // Assert
        logs.Messages.Should().Contain(x => x.Contains("Now listening on", StringComparison.Ordinal));
    }

    private static ServiceProvider Build(params (string Key, string Value)[] values)
    {
        var services = new ServiceCollection();
        services.AddValidatedSettings<SampleSettings, SampleSettingsValidator>(CreateConfiguration(values), Section);
        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(Pairs(values)).Build();
    }

    private static IEnumerable<KeyValuePair<string, string?>> Pairs(params (string Key, string Value)[] values)
    {
        return values.Select(x => new KeyValuePair<string, string?>(x.Key, x.Value));
    }

    private sealed class SampleSettings
    {
        public int Count { get; set; } = 5;

        public string? Name { get; set; } = "default";
    }

    private sealed class SampleSettingsValidator : SettingsValidatorBase<SampleSettings>
    {
        protected override string SectionName => Section;

        protected override IEnumerable<string?> GetErrors(SampleSettings settings)
        {
            yield return SettingsRules.Range(Key(nameof(settings.Count)), settings.Count, 1, 10);
            yield return SettingsRules.NotEmpty(Key(nameof(settings.Name)), settings.Name);
        }
    }

    private sealed class LogCollector : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class CollectingLogger(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (messages)
                {
                    messages.Add(formatter(state, exception));
                }
            }
        }
    }
}
