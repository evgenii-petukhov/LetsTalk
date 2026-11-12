using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LetsTalk.Server.KeepAlive.Tests;

[TestFixture]
public class KeepAliveRegistrationTests
{
    [Test]
    public void AddKeepAlive_ShouldBindSettingsAndRegisterTheHostedService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KeepAlive:StartupDelay"] = "00:00:10",
            })
            .Build();
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddKeepAlive(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IOptions<KeepAliveSettings>>().Value.StartupDelay
            .Should().Be(TimeSpan.FromSeconds(10));
        provider.GetServices<IHostedService>().Should().ContainSingle(service => service is KeepAliveService);
    }

    [Test]
    public void AddKeepAlive_WhenTargetsGiven_ShouldBindThem()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KeepAlive:Targets:Redis:Interval"] = "12:00:00",
                ["KeepAlive:Targets:MongoDB:Enabled"] = "false",
            })
            .Build();
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddKeepAlive(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var targets = provider.GetRequiredService<IOptions<KeepAliveSettings>>().Value.Targets;
        targets["Redis"].Interval.Should().Be(TimeSpan.FromHours(12));
        targets["redis"].Enabled.Should().BeTrue();
        targets["MongoDB"].Enabled.Should().BeFalse();
    }

    [Test]
    public void AddKeepAlive_WhenNoTargetRegistered_ShouldStillResolveTheHostedService()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddKeepAlive(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        // Act
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        // Assert
        hostedServices.Should().ContainSingle();
    }

    [Test]
    public void AddKeepAlive_WhenTargetRegistered_ShouldGiveItToTheHostedService()
    {
        // Arrange
        var target = new Moq.Mock<IKeepAliveTarget>();
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(target.Object);
        services.AddKeepAlive(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        // Act
        var targets = provider.GetServices<IKeepAliveTarget>();

        // Assert
        targets.Should().ContainSingle().Which.Should().BeSameAs(target.Object);
    }
}
