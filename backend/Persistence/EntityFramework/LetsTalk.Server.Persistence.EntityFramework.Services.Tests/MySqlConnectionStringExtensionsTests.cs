using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace LetsTalk.Server.Persistence.EntityFramework.Services.Tests;

[TestFixture]
public class MySqlConnectionStringExtensionsTests
{
    private const string ConnectionString = "server=localhost; database=letstalk; user=root";
    private const string ConnectionStringWithPassword = ConnectionString + "; password=old";
    private const string ConnectionStringKey = "ConnectionStrings:MySql";
    private const string PasswordKey = "MySql:Password";

    [Test]
    public void GetMySqlConnectionString_WhenPasswordIsNotConfigured_ShouldReturnConnectionStringUnchanged()
    {
        // Arrange
        var configuration = Build(new() { [ConnectionStringKey] = ConnectionString });

        // Act
        var result = configuration.GetMySqlConnectionString();

        // Assert
        result.Should().Be(ConnectionString);
    }

    [Test]
    public void GetMySqlConnectionString_WhenPasswordIsEmpty_ShouldKeepThePasswordFromTheConnectionString()
    {
        // Arrange
        var configuration = Build(new()
        {
            [ConnectionStringKey] = ConnectionStringWithPassword,
            [PasswordKey] = ""
        });

        // Act
        var result = configuration.GetMySqlConnectionString();

        // Assert
        result.Should().Be(ConnectionStringWithPassword);
    }

    [Test]
    public void GetMySqlConnectionString_WhenPasswordIsConfigured_ShouldAddItToTheConnectionString()
    {
        // Arrange
        var configuration = Build(new()
        {
            [ConnectionStringKey] = ConnectionString,
            [PasswordKey] = "secret"
        });

        // Act
        var result = new MySqlConnectionStringBuilder(configuration.GetMySqlConnectionString());

        // Assert
        result.Password.Should().Be("secret");
        result.Server.Should().Be("localhost");
        result.Database.Should().Be("letstalk");
        result.UserID.Should().Be("root");
    }

    [Test]
    public void GetMySqlConnectionString_WhenBothPasswordsExist_ShouldPreferTheConfiguredPassword()
    {
        // Arrange
        var configuration = Build(new()
        {
            [ConnectionStringKey] = ConnectionStringWithPassword,
            [PasswordKey] = "new"
        });

        // Act
        var result = new MySqlConnectionStringBuilder(configuration.GetMySqlConnectionString());

        // Assert
        result.Password.Should().Be("new");
    }

    [Test]
    public void GetMySqlConnectionString_WhenPasswordContainsSeparators_ShouldKeepItIntact()
    {
        // Arrange
        const string password = "a;b=c\"d'e";
        var configuration = Build(new()
        {
            [ConnectionStringKey] = ConnectionString,
            [PasswordKey] = password
        });

        // Act
        var result = new MySqlConnectionStringBuilder(configuration.GetMySqlConnectionString());

        // Assert
        result.Password.Should().Be(password);
        result.Database.Should().Be("letstalk");
    }

    private static IConfiguration Build(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
