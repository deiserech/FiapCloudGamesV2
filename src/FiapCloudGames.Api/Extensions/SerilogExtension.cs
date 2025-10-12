using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Filters;
using Serilog.Templates;

namespace FiapCloudGames.Api.Extensions;

public static class SerilogExtension
{
    public static IHostBuilder AddSerilog(this IHostBuilder host)
    {
        host.UseSerilog((builderContext, loggerConfiguration) =>
        {
            var minimumLogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            switch (minimumLogLevel?.ToUpperInvariant())
            {
                case "INFORMATION":
                    loggerConfiguration.MinimumLevel.Is(LogEventLevel.Information);
                    break;
                case "DEBUG":
                    loggerConfiguration.MinimumLevel.Is(LogEventLevel.Debug);
                    break;
                case "ERROR":
                    loggerConfiguration.MinimumLevel.Is(LogEventLevel.Error);
                    break;
                default:
                    loggerConfiguration.MinimumLevel.Is(LogEventLevel.Warning);
                    break;
            }

            loggerConfiguration.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Cors", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Authorization", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.HttpLogging", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Warning);
            loggerConfiguration.Enrich.WithProperty("dd_env", Environment.GetEnvironmentVariable("DD_ENV"));
            loggerConfiguration.Enrich.WithProperty("dd_service", Environment.GetEnvironmentVariable("DD_SERVICE_NAME"));
            loggerConfiguration.Enrich.WithProperty("dd_version", Environment.GetEnvironmentVariable("DD_VERSION"));
            loggerConfiguration.Enrich.FromLogContext().Filter
                .ByExcluding(Matching.WithProperty<string>("RequestPath", s => s.Contains("health")));
            loggerConfiguration.Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers()
                .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() }));

            if (environment == "local")
                loggerConfiguration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
            else
                loggerConfiguration.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
        });

        return host;
    }

}


