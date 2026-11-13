using FluentAssertions;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration.Tests;

/// <summary>
/// The API sets the cookies; FileStorage.Service and Notifications read them. The three services must carry the same
/// AuthCookie values, otherwise one of them cannot read the cookie that another one set.
/// </summary>
[TestFixture]
public class AuthCookieSectionsAgreeTests
{
    private static readonly string[] Files = ["API", "FileStorage", "Notifications"];

    [TestCaseSource(nameof(Files))]
    public void AuthCookieSection_ShouldBeListedWithTheSixDefaults(string file)
    {
        // Act
        var section = Load(file).GetSection("AuthCookie");
        var settings = section.Get<AuthCookieSettings>();

        // Assert
        section.GetChildren().Select(x => x.Key).Should().BeEquivalentTo(
            [nameof(AuthCookieSettings.Name), nameof(AuthCookieSettings.Domain), nameof(AuthCookieSettings.Secure),
             nameof(AuthCookieSettings.SameSite), nameof(AuthCookieSettings.RefreshName), nameof(AuthCookieSettings.RefreshPath)],
            $"{file}.appsettings.json");
        settings.Should().BeEquivalentTo(new AuthCookieSettings(), $"{file}.appsettings.json");
    }

    [Test]
    public void AuthCookieSections_ShouldBeEqualInTheThreeServices()
    {
        // Act
        var sections = Files.ToDictionary(x => x, x => Flatten(Load(x).GetSection("AuthCookie")));

        // Assert
        sections["FileStorage"].Should().BeEquivalentTo(sections["API"], "FileStorage.Service.appsettings.json must agree with the API");
        sections["Notifications"].Should().BeEquivalentTo(sections["API"], "Notifications.appsettings.json must agree with the API");
    }

    private static Dictionary<string, string?> Flatten(IConfigurationSection section) =>
        section.GetChildren().ToDictionary(x => x.Key, x => x.Value);

    private static IConfigurationRoot Load(string file) =>
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "Shipped", $"{file}.appsettings.json"))
            .Build();
}
