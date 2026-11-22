using System.Reflection;
using FluentAssertions;

namespace LetsTalk.Server.Lambda.Tests;

/// <summary>
/// The Lambda packages carry everything their projects reference, so they must stay small (spec 006, plan D11).
/// The retry settings classes live in <c>LetsTalk.Server.Configuration.Models</c>, which has no dependencies,
/// so the link-preview Lambda and <c>Resilience</c> must not reach the Configuration project or the Microsoft.Extensions
/// packages it brings. The image Lambda is not checked: it carries the Configuration project already,
/// through <c>FileStorage.Amazon.Services</c> (plan D11).
/// </summary>
[TestFixture]
public class LambdaDependencyTests
{
    private static readonly string[] Forbidden =
    [
        "LetsTalk.Server.Configuration",
        "Microsoft.Extensions.Options",
        "Microsoft.Extensions.Configuration"
    ];

    private static readonly Assembly[] Roots =
    [
        typeof(LinkPreview.Lambda.FakeHttpClientService).Assembly,
        typeof(Resilience.RetryingHttpHandler).Assembly
    ];

    [TestCaseSource(nameof(Roots))]
    public void Assembly_ShouldNotReferenceTheConfigurationProjectOrItsPackages(Assembly root)
    {
        // Act
        var references = ReferencedNames(root);

        // Assert
        references.Where(name => Forbidden.Contains(name)).Should().BeEmpty();
    }

    /// <summary>The names of all assemblies reachable from <paramref name="root"/>, walking through every assembly that can be loaded.</summary>
    private static HashSet<string> ReferencedNames(Assembly root)
    {
        var seen = new HashSet<string>();
        var queue = new Queue<Assembly>([root]);

        while (queue.Count > 0)
        {
            foreach (var name in queue.Dequeue().GetReferencedAssemblies())
            {
                if (name.Name is null || !seen.Add(name.Name))
                {
                    continue;
                }

                try
                {
                    queue.Enqueue(Assembly.Load(name));
                }
                catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
                {
                    // An assembly that is not in the test output is not in the Lambda package either
                }
            }
        }

        return seen;
    }
}
