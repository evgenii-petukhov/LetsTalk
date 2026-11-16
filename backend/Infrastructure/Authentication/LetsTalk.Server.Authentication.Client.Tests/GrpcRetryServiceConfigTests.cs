using FluentAssertions;
using Grpc.Core;
using LetsTalk.Server.Resilience;

namespace LetsTalk.Server.Authentication.Client.Tests;

[TestFixture]
public class GrpcRetryServiceConfigTests
{
    private static readonly string[] ReadOnlyMethods = ["GenerateJwtToken", "ValidateJwtToken", "RevokeJwtToken", "EndSession"];

    [Test]
    public void Create_ShouldListOnlyTheFourReadOnlyMethods()
    {
        // Act
        var config = GrpcRetryServiceConfig.Create(new GrpcRetrySettings());

        // Assert: nothing that starts or renews a session, or touches a login code, is repeated (spec FR-007)
        var names = config.MethodConfigs.SelectMany(x => x.Names).ToList();
        names.Should().OnlyContain(x => x.Service == JwtTokenGrpcService.Descriptor.FullName);
        names.Select(x => x.Method).Should().BeEquivalentTo(ReadOnlyMethods);
    }

    [Test]
    public void Create_ShouldTakeEveryNameFromTheGeneratedDescriptor()
    {
        // Act
        var names = GrpcRetryServiceConfig.Create(new GrpcRetrySettings()).MethodConfigs.SelectMany(x => x.Names);

        // Assert: a method that is renamed or removed in the proto file fails here, not silently in production
        names.Should().OnlyContain(x => JwtTokenGrpcService.Descriptor.FindMethodByName(x.Method!) != null);
    }

    [Test]
    public void Create_ShouldRetryOnlyUnavailable()
    {
        // Act
        var policy = GrpcRetryServiceConfig.Create(new GrpcRetrySettings()).MethodConfigs.Single().RetryPolicy!;

        // Assert
        policy.RetryableStatusCodes.Should().Equal(StatusCode.Unavailable);
    }

    [Test]
    public void Create_ShouldApplyTheSettings()
    {
        // Arrange
        var settings = new GrpcRetrySettings
        {
            MaxAttempts = 3,
            InitialBackoff = TimeSpan.FromSeconds(2),
            MaxBackoff = TimeSpan.FromSeconds(20)
        };

        // Act
        var policy = GrpcRetryServiceConfig.Create(settings).MethodConfigs.Single().RetryPolicy!;

        // Assert
        policy.MaxAttempts.Should().Be(3);
        policy.InitialBackoff.Should().Be(TimeSpan.FromSeconds(2));
        policy.MaxBackoff.Should().Be(TimeSpan.FromSeconds(20));
        policy.BackoffMultiplier.Should().Be(2);
    }

    [Test]
    public void Create_WhenMaxAttemptsIsOne_ShouldAddNoRetryPolicy()
    {
        // Act: the gRPC client refuses a policy of one attempt, and one attempt means no repeat
        var config = GrpcRetryServiceConfig.Create(new GrpcRetrySettings { MaxAttempts = 1 });

        // Assert
        config.MethodConfigs.Should().BeEmpty();
    }
}
