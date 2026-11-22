using System.Globalization;
using FluentAssertions;

namespace LetsTalk.Server.Resilience.Tests;

[TestFixture]
public class RetryEventTests
{
    [Test]
    public void Describe_ShouldNameTheTargetTheAttemptTheReasonAndThePause()
    {
        // Arrange
        var retry = new RetryEvent(2, "HTTP 503", TimeSpan.FromSeconds(2.5));

        // Act
        var line = retry.Describe("LinkPreviewLambda");

        // Assert
        line.Should().Be("Retry LinkPreviewLambda: attempt 2 after HTTP 503, next try in 2.5 s");
    }

    [Test]
    public void Describe_ShouldUseTheInvariantCultureForThePause()
    {
        // Arrange
        var retry = new RetryEvent(1, "HttpRequestException", TimeSpan.FromSeconds(1.5));
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            // Act
            var line = retry.Describe("Target");

            // Assert
            line.Should().Contain("1.5 s");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
