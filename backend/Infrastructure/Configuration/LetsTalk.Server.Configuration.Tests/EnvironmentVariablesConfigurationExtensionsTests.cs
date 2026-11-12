using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class EnvironmentVariablesConfigurationExtensionsTests
{
    private const string PrefixedVariable = "LetsTalk_TestSection__TestKey";
    private const string UnprefixedVariable = "TestSectionUnprefixed__TestKey";
    private const string OtherPrefixVariable = "OtherApp_TestSection__TestKey";

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(PrefixedVariable, null);
        Environment.SetEnvironmentVariable(UnprefixedVariable, null);
        Environment.SetEnvironmentVariable(OtherPrefixVariable, null);
    }

    [Test]
    public void AddLetsTalkEnvironmentVariables_WhenVariableHasThePrefix_ShouldReadItWithoutThePrefix()
    {
        // Arrange
        Environment.SetEnvironmentVariable(PrefixedVariable, "value");

        // Act
        var configuration = new ConfigurationBuilder().AddLetsTalkEnvironmentVariables().Build();

        // Assert
        configuration["TestSection:TestKey"].Should().Be("value");
    }

    [Test]
    public void AddLetsTalkEnvironmentVariables_WhenVariableHasNoPrefixOrAnotherPrefix_ShouldIgnoreIt()
    {
        // Arrange
        Environment.SetEnvironmentVariable(UnprefixedVariable, "value");
        Environment.SetEnvironmentVariable(OtherPrefixVariable, "value");

        // Act
        var configuration = new ConfigurationBuilder().AddLetsTalkEnvironmentVariables().Build();

        // Assert
        configuration["TestSectionUnprefixed:TestKey"].Should().BeNull();
        configuration["TestSection:TestKey"].Should().BeNull();
    }

    [Test]
    public void AddLetsTalkEnvironmentVariables_WhenAddedAfterTheJsonSource_ShouldWin()
    {
        // Arrange
        Environment.SetEnvironmentVariable(PrefixedVariable, "from-environment");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TestSection:TestKey"] = "from-file" })
            .AddLetsTalkEnvironmentVariables()
            .Build();

        // Act
        var result = configuration["TestSection:TestKey"];

        // Assert
        result.Should().Be("from-environment");
    }
}
