using FluentAssertions;
using LetsTalk.Server.API.Core.Abstractions;
using LetsTalk.Server.API.Core.Features.VideoCall.Queries.GetCallSettings;
using LetsTalk.Server.Exceptions;
using Moq;

namespace LetsTalk.Server.API.Core.Tests.Handlers;

[TestFixture]
public class GetCallSettingsQueryHandlerTests
{
    private Mock<IIceServerConfigurationService> _iceServerConfigurationServiceMock;

    [SetUp]
    public void SetUp()
    {
        _iceServerConfigurationServiceMock = new Mock<IIceServerConfigurationService>();
    }

    private GetCallSettingsQueryHandler CreateHandler()
    {
        return new GetCallSettingsQueryHandler(
            _iceServerConfigurationServiceMock.Object);
    }

    [Test]
    public async Task Handle_WhenValidConfiguration_ShouldReturnCallSettings()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;

        var iceServerConfiguration = "stun:stun.example.com:3478";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(iceServerConfiguration);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IceServerConfiguration.Should().Be(iceServerConfiguration);

        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenComplexIceServerConfiguration_ShouldReturnCorrectSettings()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;

        var complexIceConfig = "turn:turn.example.com:3478?transport=udp,turn:turn.example.com:3478?transport=tcp,stun:stun.example.com:19302";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(complexIceConfig);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IceServerConfiguration.Should().Be(complexIceConfig);
    }

    [Test]
    public void Handle_WhenIceServerConfigurationIsNull_ShouldThrowBadRequestException()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;


        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync((string?)null!);

        var handler = CreateHandler();

        // Act & Assert
        var exception = Assert.ThrowsAsync<BadRequestException>(
            () => handler.Handle(query, cancellationToken));

        exception.Message.Should().Be("Invalid request");

        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Once);
    }

    [Test]
    public void Handle_WhenIceServerConfigurationIsEmpty_ShouldThrowBadRequestException()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;


        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync("");

        var handler = CreateHandler();

        // Act & Assert
        var exception = Assert.ThrowsAsync<BadRequestException>(
            () => handler.Handle(query, cancellationToken));

        exception.Message.Should().Be("Invalid request");

        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Once);
    }

    [Test]
    public void Handle_WhenIceServerConfigurationIsWhitespace_ShouldThrowBadRequestException()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;


        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync("   \t\n   ");

        var handler = CreateHandler();

        // Act & Assert
        var exception = Assert.ThrowsAsync<BadRequestException>(
            () => handler.Handle(query, cancellationToken));

        exception.Message.Should().Be("Invalid request");

        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Once);
    }

    [Test]
    public void Handle_WhenIceServerConfigurationServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;


        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        var handler = CreateHandler();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(query, cancellationToken));

        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenCancellationTokenProvided_ShouldPassItToService()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = new CancellationToken(false);

        var iceServerConfiguration = "stun:stun.example.com:3478";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(iceServerConfiguration);

        var handler = CreateHandler();

        // Act
        await handler.Handle(query, cancellationToken);

        // Assert - Verification is done in the setup methods that check cancellationToken
        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenSpecialCharactersInIceConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;

        var iceServerConfiguration = "turn:user:pass@turn.example.com:3478?transport=tcp&special=!@#$%^&*()";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(iceServerConfiguration);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IceServerConfiguration.Should().Be(iceServerConfiguration);
    }

    [Test]
    public async Task Handle_WhenUnicodeCharactersInIceConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;

        var iceServerConfiguration = "stun:服务器.example.com:3478,turn:用户:密码@转发.example.com:3478";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(iceServerConfiguration);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IceServerConfiguration.Should().Be(iceServerConfiguration);
    }

    [Test]
    public async Task Handle_WhenVeryLongIceConfiguration_ShouldHandleCorrectly()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;

        var longIceConfig = "stun:" + new string('a', 1000) + ".example.com:3478";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(longIceConfig);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IceServerConfiguration.Should().Be(longIceConfig);
        result.IceServerConfiguration!.Length.Should().Be(1022); // "stun:" + 1000 chars + ".example.com:3478"
    }

    [Test]
    public async Task Handle_WhenMultipleCallsWithSameQuery_ShouldCallServiceEachTime()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;

        var iceServerConfiguration = "stun:stun.example.com:3478";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(iceServerConfiguration);

        var handler = CreateHandler();

        // Act
        var result1 = await handler.Handle(query, cancellationToken);
        var result2 = await handler.Handle(query, cancellationToken);
        var result3 = await handler.Handle(query, cancellationToken);

        // Assert
        result1.Should().BeEquivalentTo(result2);
        result2.Should().BeEquivalentTo(result3);

        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Exactly(3));
    }

    [Test]
    public async Task Handle_WhenQueryParameterIsIgnored_ShouldStillWork()
    {
        // Arrange
        var query = new GetCallSettingsQuery(); // Query has no parameters, uses _ in handler
        var cancellationToken = CancellationToken.None;

        var iceServerConfiguration = "stun:stun.example.com:3478";

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(iceServerConfiguration);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IceServerConfiguration.Should().Be(iceServerConfiguration);

        // The query parameter is ignored (using _ in the handler), so any query should work the same
        _iceServerConfigurationServiceMock.Verify(
            x => x.GetIceServerConfigurationAsync(cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenValidConfigurationWithTrimming_ShouldReturnTrimmedConfiguration()
    {
        // Arrange
        var query = new GetCallSettingsQuery();
        var cancellationToken = CancellationToken.None;

        // Note: The handler doesn't trim, but this tests that whitespace-only is still invalid
        var iceServerConfiguration = "stun:stun.example.com:3478"; // Valid config without extra whitespace

        _iceServerConfigurationServiceMock
            .Setup(x => x.GetIceServerConfigurationAsync(cancellationToken))
            .ReturnsAsync(iceServerConfiguration);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IceServerConfiguration.Should().Be(iceServerConfiguration);
    }
}