using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static LetsTalk.Server.Authentication.JwtTokenGrpcService;

namespace LetsTalk.Server.Authentication.Client.Tests;

[TestFixture]
public class AuthenticationClientRegistrationTests
{
    private static ServiceProvider Build(GrpcStubHandler stub, Dictionary<string, string?>? extra = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ApplicationUrls:Authentication"] = "https://auth.example",
            ["Resilience:Grpc:InitialBackoff"] = "00:00:00.100",
            ["Resilience:Grpc:MaxBackoff"] = "00:00:00.100"
        };
        foreach (var (key, value) in extra ?? [])
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthenticationClientServices(configuration);
        services.AddHttpClient(nameof(JwtTokenGrpcServiceClient)).ConfigurePrimaryHttpMessageHandler(() => stub);
        return services.BuildServiceProvider();
    }

    private static Task<ValidateJwtTokenResponse> Validate(ServiceProvider provider) =>
        provider.GetRequiredService<JwtTokenGrpcServiceClient>().ValidateJwtTokenAsync(new ValidateJwtTokenRequest { Token = "t" }).ResponseAsync;

    [Test]
    public async Task AddAuthenticationClientServices_ShouldRepeatAReadOnlyCallWhenTheServiceIsUnavailable()
    {
        // Arrange
        var stub = new GrpcStubHandler(2, new ValidateJwtTokenResponse { AccountId = "42" });
        using var provider = Build(stub);

        // Act
        var response = await Validate(provider);

        // Assert
        response.AccountId.Should().Be("42");
        stub.Calls.Should().Be(3);
    }

    [Test]
    public async Task AddAuthenticationClientServices_WhenMaxAttemptsIsConfigured_ShouldApplyIt()
    {
        // Arrange
        var stub = new GrpcStubHandler(int.MaxValue);
        using var provider = Build(stub, new Dictionary<string, string?> { ["Resilience:Grpc:MaxAttempts"] = "2" });

        // Act
        var act = () => Validate(provider);

        // Assert
        await act.Should().ThrowAsync<Grpc.Core.RpcException>();
        stub.Calls.Should().Be(2);
    }

    [Test]
    public void AddAuthenticationClientServices_WhenRetrySettingsAreWrong_ShouldFailAndNameTheKey()
    {
        // Arrange
        using var provider = Build(new GrpcStubHandler(0, new ValidateJwtTokenResponse()),
            new Dictionary<string, string?> { ["Resilience:Grpc:MaxAttempts"] = "9" });

        // Act
        var act = () => provider.GetRequiredService<IOptions<LetsTalk.Server.Resilience.GrpcRetrySettings>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*Resilience:Grpc:MaxAttempts*");
    }

    [Test]
    public void AddAuthenticationClientServices_ShouldStillResolveTheAuthenticationClient()
    {
        // Arrange
        using var provider = Build(new GrpcStubHandler(0));

        // Act
        var client = provider.GetRequiredService<LetsTalk.Server.Authentication.Abstractions.IAuthenticationClient>();

        // Assert
        client.Should().BeOfType<AuthenticationClient>();
    }
}
