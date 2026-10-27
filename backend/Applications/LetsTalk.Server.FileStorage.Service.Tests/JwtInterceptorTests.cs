using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.FileStorage.Service.GrpcInterceptors;
using LetsTalk.Server.SignPackage.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.FileStorage.Service.Tests;

[TestFixture]
public class JwtInterceptorTests
{
    private const string HttpContextKey = "__HttpContext";
    private const string AccountIdKey = "AccountId";
    private const string CookieName = "auth-token";
    private const string CookieToken = "cookie-token";
    private const string MetadataToken = "metadata-token";
    private const string AccountId = "42";
    private const string MetadataAccountId = "7";
    private const string Request = "request";
    private const string Response = "response";

    private Mock<IAuthenticationClient> _authenticationClientMock;
    private Mock<ISignPackageService> _signPackageServiceMock;
    private JwtInterceptor _interceptor;

    [SetUp]
    public void SetUp()
    {
        _authenticationClientMock = new Mock<IAuthenticationClient>();
        _signPackageServiceMock = new Mock<ISignPackageService>();
        _interceptor = new JwtInterceptor(
            _authenticationClientMock.Object,
            _signPackageServiceMock.Object,
            Options.Create(new AuthCookieSettings()));
    }

    [Test]
    public async Task UnaryServerHandler_WhenCookiePresent_ShouldAuthenticate()
    {
        // Arrange
        var context = CreateContext(cookieToken: CookieToken);
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ReturnsAsync(AccountId);

        // Act
        var response = await Invoke(context);

        // Assert
        response.Should().Be(Response);
        context.UserState[AccountIdKey].Should().Be(AccountId);
    }

    [Test]
    public async Task UnaryServerHandler_WhenOnlyMetadata_ShouldAuthenticate()
    {
        // Arrange
        var context = CreateContext(metadataToken: MetadataToken);
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(MetadataToken))
            .ReturnsAsync(MetadataAccountId);

        // Act
        var response = await Invoke(context);

        // Assert
        response.Should().Be(Response);
        context.UserState[AccountIdKey].Should().Be(MetadataAccountId);
    }

    [Test]
    public async Task UnaryServerHandler_WhenBothPresent_ShouldPreferCookie()
    {
        // Arrange
        var context = CreateContext(cookieToken: CookieToken, metadataToken: MetadataToken);
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ReturnsAsync(AccountId);
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(MetadataToken))
            .ReturnsAsync(MetadataAccountId);

        // Act
        await Invoke(context);

        // Assert
        context.UserState[AccountIdKey].Should().Be(AccountId);
        _authenticationClientMock.Verify(x => x.ValidateJwtTokenAsync(MetadataToken), Times.Never);
    }

    [Test]
    public async Task UnaryServerHandler_WhenNone_ShouldThrowUnauthorized()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var act = () => Invoke(context);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _authenticationClientMock.Verify(x => x.ValidateJwtTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UnaryServerHandler_WhenCredentialInvalid_ShouldThrowUnauthorized()
    {
        // Arrange
        var context = CreateContext(cookieToken: CookieToken);

        // Act
        var act = () => Invoke(context);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task UnaryServerHandler_WhenCookieInvalidAndMetadataValid_ShouldAuthenticateWithMetadata()
    {
        // Arrange: same rule as the API, an invalid cookie does not hide a valid explicit credential
        var context = CreateContext(cookieToken: CookieToken, metadataToken: MetadataToken);
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(MetadataToken))
            .ReturnsAsync(MetadataAccountId);

        // Act
        await Invoke(context);

        // Assert
        context.UserState[AccountIdKey].Should().Be(MetadataAccountId);
    }

    [Test]
    public async Task UnaryServerHandler_WhenCookieValidationFailsWithRpcException_ShouldFallBackToMetadata()
    {
        // Arrange: the Authentication service reports an invalid token as an RpcException
        var context = CreateContext(cookieToken: CookieToken, metadataToken: MetadataToken);
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ThrowsAsync(new RpcException(new Status(StatusCode.Unknown, "Invalid token")));
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(MetadataToken))
            .ReturnsAsync(MetadataAccountId);

        // Act
        await Invoke(context);

        // Assert
        context.UserState[AccountIdKey].Should().Be(MetadataAccountId);
    }

    [Test]
    public async Task UnaryServerHandler_WhenCookieAndMetadataInvalid_ShouldThrowUnauthorized()
    {
        // Arrange
        var context = CreateContext(cookieToken: CookieToken, metadataToken: MetadataToken);

        // Act
        var act = () => Invoke(context);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestCase(StatusCode.Unavailable)]
    [TestCase(StatusCode.DeadlineExceeded)]
    public async Task UnaryServerHandler_WhenAuthServiceUnavailable_ShouldPropagateTheStatusInsteadOfTryingTheNextCredential(StatusCode statusCode)
    {
        // Arrange: an outage is not an invalid credential
        var context = CreateContext(cookieToken: CookieToken, metadataToken: MetadataToken);
        _authenticationClientMock
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ThrowsAsync(new RpcException(new Status(statusCode, "Authentication service is not reachable")));

        // Act
        var act = () => Invoke(context);

        // Assert
        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(statusCode);
        _authenticationClientMock.Verify(x => x.ValidateJwtTokenAsync(MetadataToken), Times.Never);
    }

    private Task<string> Invoke(TestServerCallContext context) =>
        _interceptor.UnaryServerHandler(
            Request,
            context,
            (_, _) => Task.FromResult(Response));

    private static TestServerCallContext CreateContext(string? cookieToken = null, string? metadataToken = null)
    {
        // A real gRPC-web call always has an HttpContext; GetHttpContext() throws without one
        var httpContext = new DefaultHttpContext();
        if (cookieToken != null)
        {
            httpContext.Request.Headers.Cookie = $"{CookieName}={cookieToken}";
        }

        var userState = new Dictionary<object, object> { [HttpContextKey] = httpContext };

        var headers = new Metadata();
        if (metadataToken != null)
        {
            headers.Add("authorization", metadataToken);
        }

        return new TestServerCallContext(headers, userState);
    }

    // GetHttpContext() reads the HttpContext from UserState["__HttpContext"]
    private sealed class TestServerCallContext(Metadata requestHeaders, IDictionary<object, object> userState) : ServerCallContext
    {
        protected override string MethodCore => "method";
        protected override string HostCore => "host";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore { get; } = requestHeaders;
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => null!;
        protected override IDictionary<object, object> UserStateCore => userState;

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotImplementedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
