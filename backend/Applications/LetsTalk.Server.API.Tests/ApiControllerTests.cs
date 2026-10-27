using FluentAssertions;
using LetsTalk.Server.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LetsTalk.Server.API.Tests;

[TestFixture]
public class ApiControllerTests
{
    private const string TokenKey = "Token";
    private const string BareToken = "bare-jwt";
    private const string AuthorizationHeaderValue = "Bearer header-jwt";

    [Test]
    public void GetToken_WhenSessionCookieUsed_ShouldReturnBareToken()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[TokenKey] = BareToken;
        var controller = CreateController(httpContext);

        // Act
        var result = controller.ExposedToken;

        // Assert
        result.Should().Be(BareToken);
    }

    [Test]
    public void GetToken_WhenHeaderAndResolvedTokenPresent_ShouldReturnResolvedBareToken()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = AuthorizationHeaderValue;
        httpContext.Items[TokenKey] = BareToken;
        var controller = CreateController(httpContext);

        // Act
        var result = controller.ExposedToken;

        // Assert
        result.Should().Be(BareToken);
    }

    private static TestApiController CreateController(HttpContext httpContext) => new()
    {
        ControllerContext = new ControllerContext { HttpContext = httpContext }
    };

    private sealed class TestApiController : ApiController
    {
        public string ExposedToken => GetToken();
    }
}
