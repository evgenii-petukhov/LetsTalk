using System.Reflection;
using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration.Tests;

/// <summary>
/// The shipped appsettings.json files list the tunable values, and the settings classes carry the same values as
/// defaults (plan D2). This test fails when the two copies drift apart or when a key has no property
/// (a misspelled key would otherwise do nothing).
/// </summary>
[TestFixture]
public class ShippedSettingsTests
{
    private static readonly string[] Files =
    [
        "API", "Authentication", "FileStorage", "ImageProcessing", "Notifications", "ExtractImageInfo"
    ];

    private static readonly Dictionary<string, Type> Sections = new()
    {
        ["Messaging"] = typeof(MessagingSettings),
        ["Security"] = typeof(SecuritySettings),
        ["Cloudflare"] = typeof(CloudflareSettings),
        ["Caching"] = typeof(CachingSettings),
        ["ImageConstraints"] = typeof(ImageConstraints),
        ["FileStorage"] = typeof(FileStorageSettings),
        ["JwtSettings"] = typeof(JwtSettings),
        ["AuthCookie"] = typeof(AuthCookieSettings)
    };

    public static IEnumerable<string> ShippedFiles => Files;

    [Test]
    public void ShippedFiles_ShouldBeLinkedIntoTheTestProject()
    {
        foreach (var file in Files)
        {
            File.Exists(PathOf(file)).Should().BeTrue($"{file}.appsettings.json must be linked in the project file");
        }
    }

    [TestCaseSource(nameof(ShippedFiles))]
    public void ShippedSection_ShouldHaveOnlyKnownKeysWithTheDefaultValues(string file)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile(PathOf(file)).Build();

        foreach (var (sectionName, type) in Sections)
        {
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                continue;
            }

            var defaults = Activator.CreateInstance(type)!;
            var bound = Activator.CreateInstance(type)!;
            section.Bind(bound);

            foreach (var key in section.GetChildren().Select(x => x.Key))
            {
                var property = type.GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                property.Should().NotBeNull($"{file}: {sectionName}:{key} is not a property of {type.Name}");
                property!.GetValue(bound).Should().Be(property.GetValue(defaults),
                    $"{file}: {sectionName}:{key} must equal the default of {type.Name}");
            }
        }
    }

    [Test]
    public void ShippedFiles_ShouldListEveryTunableSectionOfTheirService()
    {
        // The owner keeps the shipped values visible in the files (spec FR-002)
        Keys("API", "Messaging").Should().Contain("MessagesPerPage");
        Keys("API", "Caching").Should().Contain(["MessagesCacheLifeTimeInSeconds", "ChatCacheLifeTimeInSeconds",
            "AccountCacheLifeTimeInSeconds", "ProfileCacheLifeTimeInSeconds"]);
        Keys("Authentication", "JwtSettings").Should().BeEquivalentTo(["AccessTokenLifetime", "BackgroundTokenLifetime",
            "RefreshIdleLifetime", "RefreshAbsoluteLifetime", "RefreshReuseGrace"]);
        Keys("FileStorage", "ImageConstraints").Should().HaveCount(4);
    }

    [Test]
    public void ShippedFiles_ShouldNotListASecret()
    {
        Keys("Authentication", "JwtSettings").Should().NotContain("Key");
        Keys("FileStorage", "FileStorage").Should().NotContain("BasePath");
    }

    private static IEnumerable<string> Keys(string file, string section) =>
        new ConfigurationBuilder().AddJsonFile(PathOf(file)).Build().GetSection(section).GetChildren().Select(x => x.Key);

    private static string PathOf(string file) =>
        Path.Combine(AppContext.BaseDirectory, "Shipped", $"{file}.appsettings.json");
}
