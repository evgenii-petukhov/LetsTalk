using LetsTalk.Server.Authentication.Client;
using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.API.Core;
using LetsTalk.Server.Logging;
using LetsTalk.Server.SignPackage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using System.Reflection;

namespace LetsTalk.Server.API;

public static class ApiServiceRegistration
{
    public static async Task<IServiceCollection> AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAutoMapper(
            cfg =>
            {
                cfg.LicenseKey = configuration.GetValue<string>("AutoMapper:LicenseKey");
            },
            Assembly.GetExecutingAssembly());
        await services.AddCoreServices(configuration);
        services.AddLoggingServices();
        services.AddAuthenticationClientServices(configuration);
        services.AddSignPackageServices(configuration);
        services.AddControllers();
        services.AddCors(options =>
        {
            options.AddPolicy("all", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        services.AddSwaggerGen(c =>
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", // must be lower case
                BearerFormat = "JWT"
            };
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
        });
        services.Configure<MessagingSettings>(configuration.GetSection("Messaging"));
        services.Configure<SecuritySettings>(configuration.GetSection("Security"));
        services.Configure<FeaturesSettings>(configuration.GetSection("Features"));

        return services;
    }
}
