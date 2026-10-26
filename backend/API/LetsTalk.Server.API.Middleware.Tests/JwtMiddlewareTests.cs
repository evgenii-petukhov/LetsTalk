using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.API.Middleware.Tests;

[TestFixture]
public class JwtMiddlewareTests
{
    private const string AuthorizationHeaderKey = "Authorization";
    private const string SampleToken = "token";
    private const string CookieToken = "cookie-token";
    private const string AuthorizationHeaderValue = $"Bearer {SampleToken}";
    private const string AccountIdKey = "AccountId";
    private const string TokenKey = "Token";
    private const string AccountIdValue = "1";
    private const string CookieAccountIdValue = "2";
    private const string CookieName = "auth-token";
    private const string CookieDomain = "petukhov.fyi";
    private const string ExpiredCookieDate = "expires=Thu, 01 Jan 1970";

    private JwtMiddleware _jwtMiddleware;
    private Mock<RequestDelegate> _mockRequestDelegate;
    private Mock<IAuthenticationClient> _mockAuthenticationClient;
    private AuthCookieSettings _cookieSettings;

    [SetUp]
    public void SetUp()
    {
        _mockRequestDelegate = new Mock<RequestDelegate>();
        _mockAuthenticationClient = new Mock<IAuthenticationClient>();
        _cookieSettings = new AuthCookieSettings();
        _jwtMiddleware = new JwtMiddleware(_mockRequestDelegate.Object);
    }

    [Test]
    public async Task InvokeAsync_WhenCookiePresent_ShouldSetAccountIdAndToken()
    {
        // Arrange
        var ctx = CreateContext(cookieToken: CookieToken);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ReturnsAsync(CookieAccountIdValue);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { AccountIdKey, CookieAccountIdValue },
            { TokenKey, CookieToken }
        });
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenOnlyAuthorizationHeader_ShouldSetAccountIdAndToken()
    {
        // Arrange
        var ctx = CreateContext(authorizationHeader: AuthorizationHeaderValue);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(SampleToken))
            .ReturnsAsync(AccountIdValue);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            { AccountIdKey, AccountIdValue },
            { TokenKey, SampleToken }
        });
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenBothPresentAndDifferent_ShouldUseCookie()
    {
        // Arrange
        var ctx = CreateContext(cookieToken: CookieToken, authorizationHeader: AuthorizationHeaderValue);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ReturnsAsync(CookieAccountIdValue);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(SampleToken))
            .ReturnsAsync(AccountIdValue);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items[AccountIdKey].Should().Be(CookieAccountIdValue);
        ctx.Items[TokenKey].Should().Be(CookieToken);
        _mockAuthenticationClient.Verify(x => x.ValidateJwtTokenAsync(SampleToken), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WhenHeaderHasBearerPrefix_ShouldStoreBareToken()
    {
        // Arrange
        var ctx = CreateContext(authorizationHeader: AuthorizationHeaderValue);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(SampleToken))
            .ReturnsAsync(AccountIdValue);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items[TokenKey].Should().Be(SampleToken);
    }

    [Test]
    public async Task InvokeAsync_WhenHeaderTokenInvalid_ShouldThrowUnauthorized()
    {
        // Arrange
        var ctx = CreateContext(authorizationHeader: AuthorizationHeaderValue);

        // Act
        Func<Task> f = async () => await Invoke(ctx);

        // Assert
        await f.Should().ThrowAsync<UnauthorizedAccessException>();
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WhenNoCredential_ShouldContinueWithoutAccountId()
    {
        // Arrange
        var ctx = CreateContext();

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items.Should().BeEmpty();
        ctx.Response.Headers.SetCookie.Should().BeEmpty();
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieInvalid_ShouldContinueAnonymouslyAndDeleteCookie()
    {
        // Arrange
        var ctx = CreateContext(cookieToken: CookieToken);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items.Should().BeEmpty();
        GetSetCookie(ctx).Should().Contain($"{CookieName}=;").And.Contain(ExpiredCookieDate);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieRpcException_ShouldContinueAnonymouslyAndDeleteCookie()
    {
        // Arrange
        var ctx = CreateContext(cookieToken: CookieToken);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ThrowsAsync(new RpcException(new Status(StatusCode.Unknown, "Invalid token")));

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items.Should().BeEmpty();
        GetSetCookie(ctx).Should().Contain($"{CookieName}=;").And.Contain(ExpiredCookieDate);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [TestCase(StatusCode.Unavailable)]
    [TestCase(StatusCode.DeadlineExceeded)]
    public async Task InvokeAsync_WhenCookieAndAuthServiceUnavailable_ShouldContinueAnonymouslyAndKeepCookie(StatusCode statusCode)
    {
        // Arrange
        var ctx = CreateContext(cookieToken: CookieToken);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ThrowsAsync(new RpcException(new Status(statusCode, "Authentication service is not reachable")));

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items.Should().BeEmpty();
        ctx.Response.Headers.SetCookie.Should().BeEmpty();
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieInvalid_ShouldDeleteCookieWithConfiguredDomainAndPath()
    {
        // Arrange
        _cookieSettings.Domain = CookieDomain;
        var ctx = CreateContext(cookieToken: CookieToken);

        // Act
        await Invoke(ctx);

        // Assert
        GetSetCookie(ctx).ToLowerInvariant()
            .Should().Contain($"domain={CookieDomain}")
            .And.Contain("path=/")
            .And.Contain("httponly")
            .And.Contain("secure")
            .And.Contain("samesite=strict");
    }

    [Test]
    public async Task InvokeAsync_WhenValidCookieAndInvalidHeader_ShouldUseCookie()
    {
        // Arrange
        var ctx = CreateContext(cookieToken: CookieToken, authorizationHeader: AuthorizationHeaderValue);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(CookieToken))
            .ReturnsAsync(CookieAccountIdValue);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items[AccountIdKey].Should().Be(CookieAccountIdValue);
        ctx.Items[TokenKey].Should().Be(CookieToken);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieInvalidAndHeaderValid_ShouldUseHeaderAndDeleteCookie()
    {
        // Arrange
        var ctx = CreateContext(cookieToken: CookieToken, authorizationHeader: AuthorizationHeaderValue);
        _mockAuthenticationClient
            .Setup(x => x.ValidateJwtTokenAsync(SampleToken))
            .ReturnsAsync(AccountIdValue);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Items[AccountIdKey].Should().Be(AccountIdValue);
        ctx.Items[TokenKey].Should().Be(SampleToken);
        GetSetCookie(ctx).Should().Contain($"{CookieName}=;");
    }

    private static string GetSetCookie(HttpContext ctx) => ctx.Response.Headers.SetCookie.ToString();

    private HttpContext CreateContext(string? cookieToken = null, string? authorizationHeader = null)
    {
        HttpContext ctx = new DefaultHttpContext();

        if (cookieToken != null)
        {
            ctx.Request.Headers.Cookie = $"{CookieName}={cookieToken}";
        }

        if (authorizationHeader != null)
        {
            ctx.Request.Headers[AuthorizationHeaderKey] = authorizationHeader;
        }

        return ctx;
    }

    private Task Invoke(HttpContext ctx) => _jwtMiddleware.InvokeAsync(
        ctx,
        _mockAuthenticationClient.Object,
        Options.Create(_cookieSettings));
}
