using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace LetsTalk.Server.Persistence;

public static class MySqlConnectionStringExtensions
{
    private const string ConnectionStringName = "MySql";
    private const string PasswordKey = "MySql:Password";

    /// <summary>
    /// Returns the MySQL connection string. When <c>MySql:Password</c> is set (for example through the
    /// <c>MySql__Password</c> environment variable), it is added to the connection string and wins over
    /// a password that the string already contains.
    /// </summary>
    public static string GetMySqlConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)!;
        var password = configuration[PasswordKey];

        if (string.IsNullOrEmpty(password))
        {
            return connectionString;
        }

        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            Password = password
        };
        return builder.ConnectionString;
    }
}
