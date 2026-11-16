using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using LetsTalk.Server.Resilience;
using static LetsTalk.Server.Authentication.JwtTokenGrpcService;
using static LetsTalk.Server.Authentication.LoginCodeGrpcService;

namespace LetsTalk.Server.Authentication.Client.Tests;

// The client talks to a fake service through a real gRPC channel with the retry configuration of the production client
[TestFixture]
public class AuthenticationClientRetryTests
{
    private const string AccountId = "42";
    private const string Token = "token";
    private const string RefreshToken = "refresh";

    // The shortest pauses, so the tests do not wait
    private static readonly GrpcRetrySettings Settings = new()
    {
        MaxAttempts = 4,
        InitialBackoff = TimeSpan.FromMilliseconds(10),
        MaxBackoff = TimeSpan.FromMilliseconds(10)
    };

    private static AuthenticationClient Client(GrpcStubHandler stub)
    {
        var channel = GrpcChannel.ForAddress("https://auth.example", new GrpcChannelOptions
        {
            HttpHandler = stub,
            ServiceConfig = GrpcRetryServiceConfig.Create(Settings)
        });
        return new AuthenticationClient(new JwtTokenGrpcServiceClient(channel), new LoginCodeGrpcServiceClient(channel));
    }

    [Test]
    public async Task ValidateJwtTokenAsync_WhenUnavailableTwice_ShouldSucceedOnTheThirdAttempt()
    {
        // Arrange
        var stub = new GrpcStubHandler(2, new ValidateJwtTokenResponse { AccountId = AccountId });

        // Act
        var accountId = await Client(stub).ValidateJwtTokenAsync(Token);

        // Assert
        accountId.Should().Be(AccountId);
        stub.Calls.Should().Be(3);
    }

    [Test]
    public async Task RevokeJwtTokenAsync_WhenUnavailableTwice_ShouldSucceedOnTheThirdAttempt()
    {
        // Arrange
        var stub = new GrpcStubHandler(2, new RevokeJwtTokenResponse());

        // Act
        await Client(stub).RevokeJwtTokenAsync(Token);

        // Assert
        stub.Calls.Should().Be(3);
    }

    [Test]
    public async Task GenerateJwtTokenAsync_WhenUnavailableTwice_ShouldSucceedOnTheThirdAttempt()
    {
        // Arrange
        var stub = new GrpcStubHandler(2, new GenerateJwtTokenResponse { Token = Token, ExpiresAtUnixSeconds = 1893456900 });

        // Act
        var (token, _) = await Client(stub).GenerateJwtTokenAsync(AccountId);

        // Assert
        token.Should().Be(Token);
        stub.Calls.Should().Be(3);
    }

    [Test]
    public async Task GenerateBackgroundTokenAsync_WhenUnavailableTwice_ShouldSucceedOnTheThirdAttempt()
    {
        // Arrange
        var stub = new GrpcStubHandler(2, new GenerateJwtTokenResponse { Token = Token, ExpiresAtUnixSeconds = 1893456900 });

        // Act
        var (token, _) = await Client(stub).GenerateBackgroundTokenAsync(AccountId);

        // Assert
        token.Should().Be(Token);
        stub.Calls.Should().Be(3);
    }

    [Test]
    public async Task EndSessionAsync_WhenUnavailableTwice_ShouldSucceedOnTheThirdAttempt()
    {
        // Arrange
        var stub = new GrpcStubHandler(2, new EndSessionResponse());

        // Act
        await Client(stub).EndSessionAsync(RefreshToken);

        // Assert
        stub.Calls.Should().Be(3);
    }

    [Test]
    public async Task ValidateJwtTokenAsync_WhenAllAttemptsAreUnavailable_ShouldThrowAfterTheConfiguredAttempts()
    {
        // Arrange
        var stub = new GrpcStubHandler(int.MaxValue);

        // Act
        var act = async () => await Client(stub).ValidateJwtTokenAsync(Token);

        // Assert
        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.Unavailable);
        stub.Calls.Should().Be(Settings.MaxAttempts);
    }

    [TestCase(StatusCode.Unauthenticated)]
    [TestCase(StatusCode.InvalidArgument)]
    [TestCase(StatusCode.Internal)]
    public async Task ValidateJwtTokenAsync_WhenTheServiceRefuses_ShouldNotRepeat(StatusCode status)
    {
        // Arrange
        var stub = new GrpcStubHandler(int.MaxValue, failure: status);

        // Act
        var act = async () => await Client(stub).ValidateJwtTokenAsync(Token);

        // Assert
        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(status);
        stub.Calls.Should().Be(1);
    }

    // The calls that change state in a way a repeat could not undo get exactly one attempt (spec AS3.2)
    [Test]
    public async Task CreateSessionAsync_WhenUnavailable_ShouldTryOnce()
    {
        await AssertSingleAttempt(client => client.CreateSessionAsync(AccountId));
    }

    [Test]
    public async Task RefreshSessionAsync_WhenUnavailable_ShouldTryOnce()
    {
        await AssertSingleAttempt(client => client.RefreshSessionAsync(RefreshToken));
    }

    [Test]
    public async Task GenerateLoginCodeAsync_WhenUnavailable_ShouldTryOnce()
    {
        await AssertSingleAttempt(client => client.GenerateLoginCodeAsync("user@example.com"));
    }

    [Test]
    public async Task ValidateLoginCodeAsync_WhenUnavailable_ShouldTryOnce()
    {
        await AssertSingleAttempt(client => client.ValidateLoginCodeAsync("user@example.com", 1234));
    }

    private static async Task AssertSingleAttempt(Func<AuthenticationClient, Task> call)
    {
        // Arrange: the fake service would succeed on the second attempt, so a repeat would show
        var stub = new GrpcStubHandler(1, new SessionResponse { AccessToken = Token });

        // Act
        var act = async () => await call(Client(stub));

        // Assert
        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.Unavailable);
        stub.Calls.Should().Be(1);
    }
}
