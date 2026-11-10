using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.API.Middleware.Tests;

[TestFixture]
public class OriginCheckMiddlewareTests
{
    private const string AllowedOrigin = "https://app.example.com";
    private const string OtherAllowedOrigin = "https://127.0.0.1:4200";
    private const string ForeignOrigin = "https://evil.example";
    private const string CredentialSourceKey = "CredentialSource";
    private const string CookieSource = "Cookie";
    private const string HeaderSource = "Header";
    private const string RefreshCookieName = "auth-refresh";

    private Mock<RequestDelegate> _mockRequestDelegate;
    private OriginCheckMiddleware _middleware;

    [SetUp]
    public void SetUp()
    {
        _mockRequestDelegate = new Mock<RequestDelegate>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = AllowedOrigin,
                ["Cors:AllowedOrigins:1"] = OtherAllowedOrigin
            })
            .Build();
        _middleware = new OriginCheckMiddleware(_mockRequestDelegate.Object, new AllowedOrigins(configuration));
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    public async Task InvokeAsync_WhenCookieAndStateChangingMethodAndForeignOrigin_ShouldReturn403(string method)
    {
        // Arrange
        var ctx = CreateContext(method, origin: ForeignOrigin, credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Never);
    }

    [TestCase(AllowedOrigin)]
    [TestCase(OtherAllowedOrigin)]
    public async Task InvokeAsync_WhenCookieAndPostAndAllowedOrigin_ShouldContinue(string origin)
    {
        // Arrange
        var ctx = CreateContext("POST", origin: origin, credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieAndNoOriginButAllowedReferer_ShouldContinue()
    {
        // Arrange: only the origin part of the referer counts, not its path
        var ctx = CreateContext("POST", referer: $"{AllowedOrigin}/chats/42", credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieAndNoOriginButForeignReferer_ShouldReturn403()
    {
        // Arrange
        var ctx = CreateContext("POST", referer: $"{ForeignOrigin}/page", credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieAndNoOriginNoReferer_ShouldReturn403()
    {
        // Arrange
        var ctx = CreateContext("POST", credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WhenCookieAndRefererIsNotAUrl_ShouldReturn403()
    {
        // Arrange
        var ctx = CreateContext("POST", referer: "not a url", credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [TestCase("GET")]
    [TestCase("HEAD")]
    [TestCase("OPTIONS")]
    public async Task InvokeAsync_WhenCookieAndSafeMethod_ShouldContinue(string method)
    {
        // Arrange: reading changes nothing, and a cross-site page cannot read the answer
        var ctx = CreateContext(method, origin: ForeignOrigin, credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenHeaderCredentialAndForeignOrigin_ShouldContinue()
    {
        // Arrange: an explicit credential is not sent by the browser on its own, so it cannot be forged cross-site
        var ctx = CreateContext("POST", origin: ForeignOrigin, credentialSource: HeaderSource);

        // Act
        await Invoke(ctx);

        // Assert
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenNoCredentialAndForeignOrigin_ShouldContinue()
    {
        // Arrange: for example the sign-in request, which carries no session yet
        var ctx = CreateContext("POST", origin: ForeignOrigin);

        // Act
        await Invoke(ctx);

        // Assert
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenOnlyRefreshCookieAndForeignOrigin_ShouldReturn403()
    {
        // Arrange: refresh and sign-out carry only the refresh cookie once the access cookie has expired
        var ctx = CreateContext("POST", origin: ForeignOrigin, refreshCookie: "refresh-token");

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Never);
    }

    [Test]
    public async Task InvokeAsync_WhenOnlyRefreshCookieAndAllowedOrigin_ShouldContinue()
    {
        // Arrange
        var ctx = CreateContext("POST", origin: AllowedOrigin, refreshCookie: "refresh-token");

        // Act
        await Invoke(ctx);

        // Assert
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    [Test]
    public async Task InvokeAsync_WhenHeaderCredentialAndRefreshCookieAndForeignOrigin_ShouldReturn403()
    {
        // Arrange: the browser still sends the refresh cookie, so the request carries a browser session
        var ctx = CreateContext("POST", origin: ForeignOrigin, credentialSource: HeaderSource, refreshCookie: "refresh-token");

        // Act
        await Invoke(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task InvokeAsync_WhenOriginDiffersOnlyInLetterCase_ShouldContinue()
    {
        // Arrange: host names are case-insensitive
        var ctx = CreateContext("POST", origin: AllowedOrigin.ToUpperInvariant(), credentialSource: CookieSource);

        // Act
        await Invoke(ctx);

        // Assert
        _mockRequestDelegate.Verify(x => x.Invoke(ctx), Times.Once);
    }

    private static DefaultHttpContext CreateContext(
        string method,
        string? origin = null,
        string? referer = null,
        string? credentialSource = null,
        string? refreshCookie = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;

        if (origin != null)
        {
            ctx.Request.Headers.Origin = origin;
        }

        if (referer != null)
        {
            ctx.Request.Headers.Referer = referer;
        }

        if (credentialSource != null)
        {
            ctx.Items[CredentialSourceKey] = credentialSource;
        }

        if (refreshCookie != null)
        {
            ctx.Request.Headers.Cookie = $"{RefreshCookieName}={refreshCookie}";
        }

        return ctx;
    }

    private Task Invoke(HttpContext ctx) => _middleware.InvokeAsync(ctx, Options.Create(new AuthCookieSettings()));
}
