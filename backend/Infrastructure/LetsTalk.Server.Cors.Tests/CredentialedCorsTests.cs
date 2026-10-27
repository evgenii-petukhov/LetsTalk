using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace LetsTalk.Server.Cors.Tests;

[TestFixture]
public class CredentialedCorsTests
{
    private const string PolicyName = "all";
    private const string AllowedOrigin = "https://letstalk.example.com";
    private const string SecondAllowedOrigin = "https://127.0.0.1:4200";
    private const string OtherOrigin = "https://evil.example";
    private const string GrpcStatusHeader = "grpc-status";
    private const string GrpcMessageHeader = "grpc-message";

    [Test]
    public void AddCredentialedCors_WhenOriginsConfigured_ShouldAllowOnlyThoseOrigins()
    {
        // Act
        var policy = BuildPolicy(Environments.Production, AllowedOrigin, SecondAllowedOrigin);

        // Assert
        policy.Origins.Should().BeEquivalentTo(AllowedOrigin, SecondAllowedOrigin);
        policy.Origins.Should().NotContain(OtherOrigin);
        policy.AllowAnyOrigin.Should().BeFalse();
        policy.SupportsCredentials.Should().BeTrue();
        policy.AllowAnyHeader.Should().BeTrue();
        policy.AllowAnyMethod.Should().BeTrue();
    }

    [Test]
    public void AddCredentialedCors_WhenOriginsEmptyOutsideDevelopment_ShouldThrow()
    {
        // Act
        var act = () => BuildPolicy(Environments.Production);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cors:AllowedOrigins*");
    }

    [Test]
    public void AddCredentialedCors_WhenOriginsEmptyInDevelopment_ShouldNotThrow()
    {
        // Act
        var policy = BuildPolicy(Environments.Development);

        // Assert
        policy.Origins.Should().BeEmpty();
        policy.AllowAnyOrigin.Should().BeFalse();
    }

    [Test]
    public void AddCredentialedCors_WhenExposedHeadersProvided_ShouldExposeThem()
    {
        // Act
        var policy = BuildPolicy(
            Environments.Production,
            [AllowedOrigin],
            [GrpcStatusHeader, GrpcMessageHeader]);

        // Assert
        policy.ExposedHeaders.Should().BeEquivalentTo(GrpcStatusHeader, GrpcMessageHeader);
    }

    private static CorsPolicy BuildPolicy(string environmentName, params string[] origins)
    {
        return BuildPolicy(environmentName, origins, []);
    }

    private static CorsPolicy BuildPolicy(string environmentName, string[] origins, string[] exposedHeaders)
    {
        var settings = origins
            .Select((origin, index) => new KeyValuePair<string, string?>($"Cors:AllowedOrigins:{index}", origin));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

        var services = new ServiceCollection();
        services.AddSingleton(environment.Object);
        services.AddCredentialedCors(configuration, exposedHeaders);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(PolicyName)!;
    }
}
