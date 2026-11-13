using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Configuration.Validation;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class MessagingSettingsValidatorTests
{
    private readonly MessagingSettingsValidator _validator = new();

    [TestCase(1)]
    [TestCase(20)]
    [TestCase(100)]
    public void Validate_WhenMessagesPerPageIsInsideTheRange_ShouldSucceed(int value)
    {
        // Act
        var result = _validator.Validate(null, new MessagingSettings { MessagesPerPage = value });

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(101)]
    public void Validate_WhenMessagesPerPageIsOutsideTheRange_ShouldFailAndNameTheKey(int value)
    {
        // Act
        var result = _validator.Validate(null, new MessagingSettings { MessagesPerPage = value });

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Messaging:MessagesPerPage must be between 1 and 100");
    }

    [Test]
    public void Validate_WhenSettingsAreTheDefaults_ShouldSucceed()
    {
        // Act
        var result = _validator.Validate(null, new MessagingSettings());

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
