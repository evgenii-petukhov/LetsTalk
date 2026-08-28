using LetsTalk.Server.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Globalization;

using var host = CreateDefaultBuilder().Build();
await host.RunAsync();

// https://thecodeblogger.com/2021/05/04/how-to-use-appsettings-json-config-file-with-net-console-applications/
static IHostBuilder CreateDefaultBuilder()
{
    return Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(app =>
        {
            var filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            app.AddJsonFile(filename, optional: false);
        })
        .ConfigureServices((context, services) =>
        {
            services.AddDbContext<LetsTalkDbContext>(options =>
            {
                var connectionString = context.Configuration.GetConnectionString("MySql")!;
                options
                    .UseMySQL(
                        connectionString,
                        mysqlOptions =>
                        {
                            mysqlOptions.MigrationsHistoryTable("__efmigrationshistory");
                        })
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
        })
        .UseSerilog((context, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
        });
}