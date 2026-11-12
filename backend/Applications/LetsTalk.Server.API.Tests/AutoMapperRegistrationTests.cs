using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.API.Core;
using LetsTalk.Server.Persistence.AgnosticServices;
using LetsTalk.Server.Persistence.CosmosDB.Services;
using LetsTalk.Server.Persistence.EntityFramework.Services;
using LetsTalk.Server.Persistence.MongoDB.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.API.Tests;

[TestFixture]
public class AutoMapperRegistrationTests
{
    private const string DatabaseModeKey = "Features:DatabaseMode";

    [Test]
    public async Task AddApiServices_ShouldRegisterAutoMapperOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DatabaseModeKey] = "MySql",
                ["ConnectionStrings:MySql"] = "server=localhost; database=letstalk; user=root; password=test",
                ["Kafka:Url"] = "127.0.0.1:9092",
                ["Topics:Notification"] = "test-notification-topic",
                ["Topics:LinkPreviewRequest"] = "test-link-preview-request-topic",
                ["Topics:ImageResizeRequest"] = "test-image-resize-request-topic",
                ["Topics:RemoveImageRequest"] = "test-remove-image-request-topic",
                ["Topics:SendEmailRequest"] = "test-send-email-request-topic",
            })
            .Build();

        // Act
        await services.AddApiServices(configuration);

        // Assert
        services.Where(descriptor => descriptor.ServiceType == typeof(IMapper)).Should().ContainSingle();
    }

    [TestCase("MongoDB", typeof(MongoDBServicesRegistration))]
    [TestCase("CosmosDB", typeof(CosmosDBServicesRegistration))]
    [TestCase("MySql", typeof(EntityFrameworkServiceRegistration))]
    [TestCase(null, typeof(EntityFrameworkServiceRegistration))]
    public void GetMappingProfilesAssembly_ShouldFollowTheDatabaseMode(string? databaseMode, Type expectedMarker)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [DatabaseModeKey] = databaseMode })
            .Build();

        // Act
        var assembly = PersistenceAgnosticServicesRegistration.GetMappingProfilesAssembly(configuration);

        // Assert
        assembly.Should().BeSameAs(expectedMarker.Assembly);
    }
}
