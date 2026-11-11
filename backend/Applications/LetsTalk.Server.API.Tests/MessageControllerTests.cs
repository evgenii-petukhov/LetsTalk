using FluentAssertions;
using LetsTalk.Server.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LetsTalk.Server.API.Tests;

[TestFixture]
public class MessageControllerTests
{
    private const string MarkAsReadTemplate = "MarkAsRead";
    private const string SetLinkPreviewTemplate = "SetLinkPreview";
    private const string SetImagePreviewTemplate = "SetImagePreview";

    [TestCase(nameof(MessageController.MarkAsReadAsync), MarkAsReadTemplate)]
    [TestCase(nameof(MessageController.SetLinkPreviewAsync), SetLinkPreviewTemplate)]
    [TestCase(nameof(MessageController.SetImagePreviewAsync), SetImagePreviewTemplate)]
    public void PartialUpdateAction_ShouldBePatchWithTheSameTemplate(string actionName, string expectedTemplate)
    {
        // Act
        var attributes = GetHttpMethodAttributes(actionName);

        // Assert
        attributes.Should().ContainSingle()
            .Which.Should().BeOfType<HttpPatchAttribute>()
            .Which.Template.Should().Be(expectedTemplate);
    }

    [Test]
    public void MessageController_ShouldHaveNoPutActions()
    {
        // Act
        var putActions = typeof(MessageController)
            .GetMethods()
            .Where(method => method.GetCustomAttributes(inherit: true).OfType<HttpPutAttribute>().Any())
            .Select(method => method.Name);

        // Assert
        putActions.Should().BeEmpty();
    }

    private static IEnumerable<HttpMethodAttribute> GetHttpMethodAttributes(string actionName) =>
        typeof(MessageController)
            .GetMethod(actionName)!
            .GetCustomAttributes(inherit: true)
            .OfType<HttpMethodAttribute>();
}
