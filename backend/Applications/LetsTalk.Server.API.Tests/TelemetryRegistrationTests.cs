using FluentAssertions;
using LetsTalk.Server.API.Core.Services;
using LetsTalk.Server.Telemetry.Abstractions;
using LetsTalk.Server.Telemetry.AgnosticServices;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.API.Tests;

[TestFixture]
public class TelemetryRegistrationTests
{
    private const string ModeKey = "Features:Telemetry";
    private const string ConnectionStringKey = "ApplicationInsights:ConnectionString";
    private const string ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000";

    [TestCase("None")]
    [TestCase("none")]
    [TestCase("")]
    [TestCase(null)]
    public void AddTelemetryAgnosticServices_WhenTelemetryIsOff_ShouldRegisterTheNullService(string? mode)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTelemetryAgnosticServices(CreateConfiguration(mode, connectionString: null));

        // Assert
        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(ITelemetryService)).Subject;
        descriptor.ImplementationType.Should().Be<NullTelemetryService>();
        services.Should().NotContain(d => d.ServiceType == typeof(TelemetryConfiguration));
    }

    [Test]
    public void AddTelemetryAgnosticServices_WhenAzureAndConnectionStringIsSet_ShouldRegisterTheAzureService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTelemetryAgnosticServices(CreateConfiguration("Azure", ConnectionString));

        // Assert
        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(ITelemetryService)).Subject;
        descriptor.ImplementationType.Should().Be<TelemetryService>();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void AddTelemetryAgnosticServices_WhenAzureAndConnectionStringIsMissing_ShouldExplainHowToFixIt(string? connectionString)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddTelemetryAgnosticServices(CreateConfiguration("Azure", connectionString));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{ConnectionStringKey}*{ModeKey}*None*");
    }

    [Test]
    public void AddTelemetryAgnosticServices_WhenModeIsUnknown_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddTelemetryAgnosticServices(CreateConfiguration("Datadog", ConnectionString));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Datadog*");
    }

    [Test]
    public void NullTelemetryService_WhenCalled_ShouldDoNothing()
    {
        // Arrange
        var service = new NullTelemetryService();

        // Act
        var act = () => service.TrackConnectionEstablished("call", "chat", "account", new());

        // Assert
        act.Should().NotThrow();
    }

    private static IConfiguration CreateConfiguration(string? mode, string? connectionString)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ModeKey] = mode,
                [ConnectionStringKey] = connectionString
            })
            .Build();
    }
}
