using FluentAssertions;
using LetsTalk.Server.KeepAlive;
using LetsTalk.Server.Persistence.MongoDB.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace LetsTalk.Server.Persistence.MongoDB.Services.Tests;

[TestFixture]
public class MongoDBKeepAliveTargetTests
{
    private const string AdminDatabaseName = "admin";
    private const string PingCommandName = "ping";
    private const string FailureMessage = "the cluster is paused";

    private Mock<IMongoClient> _clientMock;
    private Mock<IMongoDatabase> _databaseMock;
    private Command<BsonDocument>? _sentCommand;
    private MongoDBKeepAliveTarget _target;

    [SetUp]
    public void SetUp()
    {
        _sentCommand = null;
        _databaseMock = new Mock<IMongoDatabase>();
        _databaseMock
            .Setup(x => x.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .Callback<Command<BsonDocument>, ReadPreference, CancellationToken>((command, _, _) => _sentCommand = command)
            .ReturnsAsync(new BsonDocument("ok", 1));
        _clientMock = new Mock<IMongoClient>();
        _clientMock
            .Setup(x => x.GetDatabase(AdminDatabaseName, It.IsAny<MongoDatabaseSettings>()))
            .Returns(_databaseMock.Object);

        _target = new MongoDBKeepAliveTarget(_clientMock.Object);
    }

    [Test]
    public void Name_ShouldBeMongoDB()
    {
        _target.Name.Should().Be("MongoDB");
    }

    [Test]
    public async Task KeepAliveAsync_ShouldRunThePingCommandOnTheAdminDatabase()
    {
        // Act
        await _target.KeepAliveAsync(CancellationToken.None);

        // Assert
        _clientMock.Verify(x => x.GetDatabase(AdminDatabaseName, It.IsAny<MongoDatabaseSettings>()), Times.Once);
        var command = _sentCommand.Should().BeOfType<BsonDocumentCommand<BsonDocument>>().Subject;
        command.Document.Equals(new BsonDocument(PingCommandName, 1)).Should().BeTrue();
    }

    [Test]
    public async Task KeepAliveAsync_WhenMongoThrows_ShouldPropagate()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException(FailureMessage));

        // Act
        var act = () => _target.KeepAliveAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>().WithMessage(FailureMessage);
    }

    [Test]
    public async Task KeepAliveAsync_WhenAgainstARealServer_ShouldBeAccepted()
    {
        // Arrange
        using var runner = MongoDbRunner.Start();
        var target = new MongoDBKeepAliveTarget(new MongoClient(runner.ConnectionString));

        // Act
        var act = () => target.KeepAliveAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Test]
    public void AddMongoDBRepository_ShouldRegisterTheMongoDBKeepAliveTarget()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDB"] = "mongodb://localhost:27017",
            })
            .Build();

        // Act
        services.AddMongoDBRepository(configuration);

        // Assert
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IKeepAliveTarget)
            && descriptor.ImplementationType == typeof(MongoDBKeepAliveTarget));
    }
}
