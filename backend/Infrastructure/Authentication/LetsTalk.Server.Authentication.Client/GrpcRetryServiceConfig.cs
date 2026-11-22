using Grpc.Core;
using Grpc.Net.Client.Configuration;
using LetsTalk.Server.Resilience;

namespace LetsTalk.Server.Authentication.Client;

/// <summary>
/// The gRPC retry rule of spec 006 (FR-007):
/// a call is repeated only when the Authentication service cannot be reached (status "unavailable"),
/// and only for the calls that a repeat cannot harm.
/// The other calls get one attempt:
/// - starting a session (a repeat leaves an orphan session),
/// - renewing it (a repeat reuses a replaced token and ends the chain),
/// - generating and checking a login code (a repeat answers "already created", so no e-mail is sent).
/// </summary>
public static class GrpcRetryServiceConfig
{
    /// <summary>The methods of <see cref="JwtTokenGrpcService"/> that may be repeated.</summary>
    private static readonly string[] RepeatableMethods = [
        "GenerateJwtToken",
        "ValidateJwtToken",
        "RevokeJwtToken",
        "EndSession"];

    private const double BackoffMultiplier = 2;

    public static ServiceConfig Create(GrpcRetrySettings settings)
    {
        var config = new ServiceConfig();

        // One attempt means no repeat, and the gRPC client refuses a policy with one attempt
        if (settings.MaxAttempts <= 1)
        {
            return config;
        }

        var methodConfig = new MethodConfig
        {
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = settings.MaxAttempts,
                InitialBackoff = settings.InitialBackoff,
                MaxBackoff = settings.MaxBackoff,
                BackoffMultiplier = BackoffMultiplier,
                RetryableStatusCodes = { StatusCode.Unavailable }
            }
        };

        // The names come from the generated descriptor, so a method that is renamed in the proto file fails here
        foreach (var method in RepeatableMethods)
        {
            var descriptor = JwtTokenGrpcService.Descriptor.FindMethodByName(method)
                ?? throw new InvalidOperationException($"{JwtTokenGrpcService.Descriptor.FullName} has no method {method}.");
            methodConfig.Names.Add(new MethodName
            {
                Service = JwtTokenGrpcService.Descriptor.FullName,
                Method = descriptor.Name
            });
        }

        config.MethodConfigs.Add(methodConfig);
        return config;
    }
}
