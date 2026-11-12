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
                ["KeepAlive:Interval"] = "12:00:00",
            })
            .Build();
        var services = new ServiceCollection().AddLogging();

        // Act
        services.AddKeepAlive(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IOptions<KeepAliveSettings>>().Value.Interval
            .Should().Be(TimeSpan.FromHours(12));
        provider.GetServices<IHostedService>().Should().ContainSingle(service => service is KeepAliveService);
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
