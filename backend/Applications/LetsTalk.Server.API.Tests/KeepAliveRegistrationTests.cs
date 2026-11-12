using FluentAssertions;
using LetsTalk.Server.KeepAlive;
using LetsTalk.Server.Persistence.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LetsTalk.Server.API.Tests;

[TestFixture]
public class KeepAliveRegistrationTests
{
    private const string CachingModeKey = "Features:CachingMode";
    private const string DatabaseModeKey = "Features:DatabaseMode";
    private const string MySqlConnectionStringKey = "ConnectionStrings:MySql";
    private const string MySqlConnectionString = "server=localhost; database=letstalk; user=root; password=test";

    [Test]
    public async Task AddApiServices_WhenMemoryCacheAndMySql_ShouldRegisterNoKeepAliveTarget()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        await services.AddApiServices(CreateConfiguration(cachingMode: "memory"));

        // Assert: nothing hosted to keep alive (spec AS4.1, AS4.2)
        services.Where(descriptor => descriptor.ServiceType == typeof(IKeepAliveTarget)).Should().BeEmpty();
    }

    [Test]
    public async Task AddApiServices_WhenRedisCache_ShouldRegisterTheRedisTargetOnly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        await services.AddApiServices(CreateConfiguration(cachingMode: "redis"));

        // Assert
        services.Where(descriptor => descriptor.ServiceType == typeof(IKeepAliveTarget))
            .Should().ContainSingle()
            .Which.ImplementationType.Should().Be(typeof(RedisKeepAliveTarget));
    }

    [TestCase("memory")]
    [TestCase("redis")]
    public async Task AddApiServices_ShouldRegisterTheKeepAliveHostedService(string cachingMode)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        await services.AddApiServices(CreateConfiguration(cachingMode));

        // Assert: the API is the only service that sends the signals (spec FR-007)
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(KeepAliveService));
    }

    private static IConfiguration CreateConfiguration(string cachingMode) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [CachingModeKey] = cachingMode,
                [DatabaseModeKey] = "MySql",
                [MySqlConnectionStringKey] = MySqlConnectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Kafka:Url"] = "127.0.0.1:9092",
                ["Topics:Notification"] = "test-notification-topic",
                ["Topics:LinkPreviewRequest"] = "test-link-preview-request-topic",
                ["Topics:ImageResizeRequest"] = "test-image-resize-request-topic",
                ["Topics:RemoveImageRequest"] = "test-remove-image-request-topic",
                ["Topics:SendEmailRequest"] = "test-send-email-request-topic",
            })
            .Build();
}
