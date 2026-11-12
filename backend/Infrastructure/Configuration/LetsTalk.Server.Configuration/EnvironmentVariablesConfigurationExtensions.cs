using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration;

public static class EnvironmentVariablesConfigurationExtensions
{
    /// <summary>
    /// Prefix of the environment variables that belong to LetsTalk, for example
    /// <c>LetsTalk_JwtSettings__Key</c>. The prefix is removed from the configuration key.
    /// </summary>
    public const string EnvironmentVariablePrefix = "LetsTalk_";

    /// <summary>
    /// Reads the environment variables that start with <see cref="EnvironmentVariablePrefix"/>. Add it
    /// after the other sources, so these values win over the files and over the unprefixed variables
    /// that the default host builder reads.
    /// </summary>
    public static IConfigurationBuilder AddLetsTalkEnvironmentVariables(this IConfigurationBuilder builder)
    {
        return builder.AddEnvironmentVariables(EnvironmentVariablePrefix);
    }
}
