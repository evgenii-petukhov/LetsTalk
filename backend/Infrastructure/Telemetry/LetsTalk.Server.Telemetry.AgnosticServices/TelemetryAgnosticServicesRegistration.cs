using LetsTalk.Server.Telemetry.Abstractions;
using LetsTalk.Server.Telemetry.Azure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LetsTalk.Server.Telemetry.AgnosticServices;

public static class TelemetryAgnosticServicesRegistration
{
    private const string ModeKey = "Features:Telemetry";
    private const string ConnectionStringKey = "ApplicationInsights:ConnectionString";
    private const string AzureMode = "Azure";
    private const string NoneMode = "None";

    public static IServiceCollection AddTelemetryAgnosticServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        switch (GetMode(configuration))
        {
            case AzureMode:
                if (string.IsNullOrWhiteSpace(configuration[ConnectionStringKey]))
                {
                    throw new InvalidOperationException(
                        $"{ModeKey} is {AzureMode}, but {ConnectionStringKey} is not set. " +
                        $"Set it, or set {ModeKey} to {NoneMode} to run without telemetry.");
                }

                services.AddAzureTelemetryServices();
                break;
            default:
                services.AddScoped<ITelemetryService, NullTelemetryService>();
                break;
        }

        return services;
    }

    public static WebApplication UseTelemetryFlush(this WebApplication app, IConfiguration configuration)
    {
        return GetMode(configuration) == AzureMode
            ? app.UseAzureTelemetryFlush()
            : app;
    }

    private static string GetMode(IConfiguration configuration)
    {
        var mode = configuration[ModeKey];

        if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, NoneMode, StringComparison.OrdinalIgnoreCase))
        {
            return NoneMode;
        }

        if (string.Equals(mode, AzureMode, StringComparison.OrdinalIgnoreCase))
        {
            return AzureMode;
        }

        throw new InvalidOperationException(
            $"{ModeKey} has the unknown value '{mode}'. Use {AzureMode} or {NoneMode}.");
    }
}
