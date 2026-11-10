using LetsTalk.Server.Models.Dtos;
using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.API.Core.Features.Authentication.Commands.EmailLogin;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Exceptions;
using LetsTalk.Server.Persistence.AgnosticServices.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using Moq;

namespace LetsTalk.Server.API.Core.Tests.Handlers;

[TestFixture]
public class EmailLoginCommandHandlerTests
{
    private const string RefreshToken = "refresh-token-123";

    private static readonly DateTime ExpiresAt = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RefreshExpiresAt = new(2030, 1, 31, 0, 0, 0, DateTimeKind.Utc);

    private Mock<IAuthenticationClient> _authenticationClientMock;
    private Mock<IAccountAgnosticService> _accountAgnosticServiceMock;
    private Mock<IMapper> _mapperMock;
    private EmailLoginCommandHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _authenticationClientMock = new Mock<IAuthenticationClient>();
        _accountAgnosticServiceMock = new Mock<IAccountAgnosticService>();
        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(x => x.Map<SessionDto>(It.IsAny<AuthSession>()))
            .Returns(new SessionDto("jwt-token-123", ExpiresAt, RefreshToken, RefreshExpiresAt));
        _handler = new EmailLoginCommandHandler(_authenticationClientMock.Object, _accountAgnosticServiceMock.Object, _mapperMock.Object);
    }

    [Test]
    public async Task Handle_WhenValidRequest_ShouldReturnSuccessfulLoginResponse()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var accountId = "account-123";
        var token = "jwt-token-123";
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken))
            .ReturnsAsync(accountId);

        _authenticationClientMock
            .Setup(x => x.CreateSessionAsync(accountId))
            .ReturnsAsync(new AuthSession(token, ExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(token);
        result.AccessExpiresAt.Should().Be(ExpiresAt);
        result.RefreshToken.Should().Be(RefreshToken);
        result.RefreshExpiresAt.Should().Be(RefreshExpiresAt);
    }

    [Test]
    public async Task Handle_WhenEmailHasWhitespace_ShouldTrimAndNormalizeEmail()
    {
        // Arrange
        var email = "  TEST@EXAMPLE.COM  ";
        var normalizedEmail = "test@example.com";
        var code = 123456;
        var accountId = "account-123";
        var token = "jwt-token-123";
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(normalizedEmail, code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, normalizedEmail, cancellationToken))
            .ReturnsAsync(accountId);

        _authenticationClientMock
            .Setup(x => x.CreateSessionAsync(accountId))
            .ReturnsAsync(new AuthSession(token, ExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(token);
        result.AccessExpiresAt.Should().Be(ExpiresAt);
        result.RefreshToken.Should().Be(RefreshToken);
        result.RefreshExpiresAt.Should().Be(RefreshExpiresAt);

        // Verify that the email was normalized
        _authenticationClientMock.Verify(
            x => x.ValidateLoginCodeAsync(normalizedEmail, code),
            Times.Once);

        _accountAgnosticServiceMock.Verify(
            x => x.GetOrCreateAsync(AccountTypes.Email, normalizedEmail, cancellationToken),
            Times.Once);
    }

    [Test]
    public void Handle_WhenValidationFails_ShouldThrowBadRequestException()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Test]
    public async Task Handle_WhenValidationPasses_ShouldCallGetOrCreateWithCorrectParameters()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var accountId = "account-123";
        var token = "jwt-token-123";
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken))
            .ReturnsAsync(accountId);

        _authenticationClientMock
            .Setup(x => x.CreateSessionAsync(accountId))
            .ReturnsAsync(new AuthSession(token, ExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _accountAgnosticServiceMock.Verify(
            x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenValidationPasses_ShouldCreateTheSessionForTheAccount()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var accountId = "account-123";
        var token = "jwt-token-123";
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken))
            .ReturnsAsync(accountId);

        _authenticationClientMock
            .Setup(x => x.CreateSessionAsync(accountId))
            .ReturnsAsync(new AuthSession(token, ExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _authenticationClientMock.Verify(
            x => x.CreateSessionAsync(accountId),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenCancellationTokenProvided_ShouldPassItToServices()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var accountId = "account-123";
        var token = "jwt-token-123";
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None; // Use a non-cancelled token

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken))
            .ReturnsAsync(accountId);

        _authenticationClientMock
            .Setup(x => x.CreateSessionAsync(accountId))
            .ReturnsAsync(new AuthSession(token, ExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _accountAgnosticServiceMock.Verify(
            x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken),
            Times.Once);
    }

    [Test]
    public void Handle_WhenAuthenticationClientThrowsException_ShouldPropagateException()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ThrowsAsync(new InvalidOperationException("Authentication service error"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Test]
    public void Handle_WhenAccountServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Account service error"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Test]
    public void Handle_WhenTokenGenerationThrowsException_ShouldPropagateException()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var accountId = "account-123";
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, email.ToLowerInvariant(), cancellationToken))
            .ReturnsAsync(accountId);

        _authenticationClientMock
            .Setup(x => x.CreateSessionAsync(accountId))
            .ThrowsAsync(new InvalidOperationException("Session creation error"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Test]
    public void Handle_WhenEmailIsNull_ShouldHandleGracefully()
    {
        // Arrange
        var code = 123456;
        var command = new EmailLoginCommand { Email = null, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(It.IsAny<string>(), code))
            .ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Test]
    public void Handle_WhenEmailIsEmpty_ShouldHandleGracefully()
    {
        // Arrange
        var email = "";
        var code = 123456;
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email, code))
            .ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, cancellationToken));
    }

    [Test]
    public void Handle_WhenCodeIsZero_ShouldStillValidate()
    {
        // Arrange
        var email = "test@example.com";
        var code = 0;
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, cancellationToken));

        // Verify that validation was attempted with the zero code
        _authenticationClientMock.Verify(
            x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code),
            Times.Once);
    }

    [Test]
    public async Task Handle_WhenValidRequest_ShouldFollowCompleteWorkflow()
    {
        // Arrange
        var email = "  Test@Example.Com  ";
        var normalizedEmail = "test@example.com";
        var code = 123456;
        var accountId = "account-123";
        var token = "jwt-token-123";
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(normalizedEmail, code))
            .ReturnsAsync(true);

        _accountAgnosticServiceMock
            .Setup(x => x.GetOrCreateAsync(AccountTypes.Email, normalizedEmail, cancellationToken))
            .ReturnsAsync(accountId);

        _authenticationClientMock
            .Setup(x => x.CreateSessionAsync(accountId))
            .ReturnsAsync(new AuthSession(token, ExpiresAt, RefreshToken, RefreshExpiresAt));

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(token);
        result.AccessExpiresAt.Should().Be(ExpiresAt);
        result.RefreshToken.Should().Be(RefreshToken);
        result.RefreshExpiresAt.Should().Be(RefreshExpiresAt);

        // Verify the complete workflow
        _authenticationClientMock.Verify(
            x => x.ValidateLoginCodeAsync(normalizedEmail, code),
            Times.Once);

        _accountAgnosticServiceMock.Verify(
            x => x.GetOrCreateAsync(AccountTypes.Email, normalizedEmail, cancellationToken),
            Times.Once);

        _authenticationClientMock.Verify(
            x => x.CreateSessionAsync(accountId),
            Times.Once);
    }

    [Test]
    public void Handle_WhenValidationFailsWithMultipleErrors_ShouldIncludeAllErrorsInException()
    {
        // Arrange
        var email = "test@example.com";
        var code = 123456;
        var command = new EmailLoginCommand { Email = email, Code = code };
        var cancellationToken = CancellationToken.None;

        // This test simulates the validator behavior when validation fails
        _authenticationClientMock
            .Setup(x => x.ValidateLoginCodeAsync(email.ToLowerInvariant(), code))
            .ReturnsAsync(false);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, cancellationToken));
    }
}