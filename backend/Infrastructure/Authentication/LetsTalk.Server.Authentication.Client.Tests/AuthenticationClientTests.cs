using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Authentication.Client;
using Moq;
using static LetsTalk.Server.Authentication.JwtTokenGrpcService;
using static LetsTalk.Server.Authentication.LoginCodeGrpcService;

namespace LetsTalk.Server.Authentication.Client.Tests;

[TestFixture]
public class AuthenticationClientTests
{
    private const string AccountId = "42";
    private const string AccessToken = "access-token";
    private const string RefreshToken = "refresh-token";
    private const string NewRefreshToken = "new-refresh-token";
    private const long AccessExpiresAt = 1893456900;
    private const long RefreshExpiresAt = 1896048000;

    private Mock<JwtTokenGrpcServiceClient> _tokenClientMock = null!;
    private AuthenticationClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _tokenClientMock = new Mock<JwtTokenGrpcServiceClient>();
        _client = new AuthenticationClient(_tokenClientMock.Object, Mock.Of<LoginCodeGrpcServiceClient>());
    }

    [Test]
    public async Task CreateSessionAsync_WhenCalled_ShouldMapTokensAndExpiry()
    {
        // Arrange
        SetupCreateSession(new SessionResponse
        {
            AccessToken = AccessToken,
            AccessExpiresAtUnixSeconds = AccessExpiresAt,
            RefreshToken = RefreshToken,
            RefreshExpiresAtUnixSeconds = RefreshExpiresAt
        });

        // Act
        var session = await _client.CreateSessionAsync(AccountId);

        // Assert
        session.AccessToken.Should().Be(AccessToken);
        session.AccessExpiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(AccessExpiresAt).UtcDateTime);
        session.RefreshToken.Should().Be(RefreshToken);
        session.RefreshExpiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(RefreshExpiresAt).UtcDateTime);
        _tokenClientMock.Verify(x => x.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r => r.AccountId == AccountId),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RefreshSessionAsync_WhenRotated_ShouldMapBothTokens()
    {
        // Arrange
        SetupRefreshSession(Call(new SessionResponse
        {
            AccessToken = AccessToken,
            AccessExpiresAtUnixSeconds = AccessExpiresAt,
            RefreshToken = NewRefreshToken,
            RefreshExpiresAtUnixSeconds = RefreshExpiresAt
        }));

        // Act
        var session = await _client.RefreshSessionAsync(RefreshToken);

        // Assert
        session.Should().NotBeNull();
        session!.RefreshToken.Should().Be(NewRefreshToken);
        session.RefreshExpiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(RefreshExpiresAt).UtcDateTime);
        _tokenClientMock.Verify(x => x.RefreshSessionAsync(
            It.Is<RefreshSessionRequest>(r => r.RefreshToken == RefreshToken),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RefreshSessionAsync_WhenReusedWithinGrace_ShouldReturnAccessTokenOnly()
    {
        // Arrange: the server sends an empty refresh token and 0 as its expiry
        SetupRefreshSession(Call(new SessionResponse
        {
            AccessToken = AccessToken,
            AccessExpiresAtUnixSeconds = AccessExpiresAt
        }));

        // Act
        var session = await _client.RefreshSessionAsync(RefreshToken);

        // Assert
        session!.AccessToken.Should().Be(AccessToken);
        session.RefreshToken.Should().BeNull();
        session.RefreshExpiresAt.Should().BeNull();
    }

    [Test]
    public async Task RefreshSessionAsync_WhenRefreshTokenRejected_ShouldReturnNull()
    {
        // Arrange
        SetupRefreshSession(FailedCall(StatusCode.Unauthenticated));

        // Act
        var session = await _client.RefreshSessionAsync(RefreshToken);

        // Assert
        session.Should().BeNull();
    }

    [Test]
    public async Task RefreshSessionAsync_WhenAuthenticationServiceUnavailable_ShouldThrow()
    {
        // Arrange: an outage is not a rejection, the caller must not sign the user out
        SetupRefreshSession(FailedCall(StatusCode.Unavailable));

        // Act
        var act = () => _client.RefreshSessionAsync(RefreshToken);

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.Unavailable);
    }

    [Test]
    public async Task EndSessionAsync_WhenCalled_ShouldSendTheRefreshToken()
    {
        // Arrange
        _tokenClientMock
            .Setup(x => x.EndSessionAsync(
                It.IsAny<EndSessionRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Call(new EndSessionResponse()));

        // Act
        await _client.EndSessionAsync(RefreshToken);

        // Assert
        _tokenClientMock.Verify(x => x.EndSessionAsync(
            It.Is<EndSessionRequest>(r => r.RefreshToken == RefreshToken),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GenerateBackgroundTokenAsync_WhenCalled_ShouldRequestABackgroundToken()
    {
        // Arrange
        _tokenClientMock
            .Setup(x => x.GenerateJwtTokenAsync(
                It.IsAny<GenerateJwtTokenRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Call(new GenerateJwtTokenResponse { Token = AccessToken, ExpiresAtUnixSeconds = AccessExpiresAt }));

        // Act
        var (token, expiresAt) = await _client.GenerateBackgroundTokenAsync(AccountId);

        // Assert
        token.Should().Be(AccessToken);
        expiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(AccessExpiresAt).UtcDateTime);
        _tokenClientMock.Verify(x => x.GenerateJwtTokenAsync(
            It.Is<GenerateJwtTokenRequest>(r => r.AccountId == AccountId && r.Kind == TokenKind.Background),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GenerateJwtTokenAsync_WhenCalled_ShouldRequestAnAccessToken()
    {
        // Arrange
        _tokenClientMock
            .Setup(x => x.GenerateJwtTokenAsync(
                It.IsAny<GenerateJwtTokenRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Call(new GenerateJwtTokenResponse { Token = AccessToken, ExpiresAtUnixSeconds = AccessExpiresAt }));

        // Act
        await _client.GenerateJwtTokenAsync(AccountId);

        // Assert
        _tokenClientMock.Verify(x => x.GenerateJwtTokenAsync(
            It.Is<GenerateJwtTokenRequest>(r => r.Kind == TokenKind.Access),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupCreateSession(SessionResponse response)
    {
        _tokenClientMock
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<CreateSessionRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Call(response));
    }

    private void SetupRefreshSession(AsyncUnaryCall<SessionResponse> call)
    {
        _tokenClientMock
            .Setup(x => x.RefreshSessionAsync(
                It.IsAny<RefreshSessionRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(call);
    }

    private static AsyncUnaryCall<T> Call<T>(T response)
    {
        return new AsyncUnaryCall<T>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
    }

    private static AsyncUnaryCall<SessionResponse> FailedCall(StatusCode statusCode)
    {
        return new AsyncUnaryCall<SessionResponse>(
            Task.FromException<SessionResponse>(new RpcException(new Status(statusCode, "failed"))),
            Task.FromResult(new Metadata()),
            () => new Status(statusCode, "failed"),
            () => [],
            () => { });
    }
}
