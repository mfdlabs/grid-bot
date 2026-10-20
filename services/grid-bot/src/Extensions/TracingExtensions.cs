namespace Grid.Bot.Extensions;

using System;
using System.Reflection;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;

using Logging;

using Telemetry;

/// <summary>
/// Extension methods for starting tracing.
/// </summary>
public static class TracingExtensions
{
    private static TracerProvider _tracerProvider;

    /// <summary>
    /// Starts exporting traces when <see cref="TelemetryOptions.TracingEnabled"/> is set.
    /// </summary>
    /// <remarks>
    /// Call this before the web, gRPC, and Discord clients start so their requests are instrumented.
    /// The HTTP server, gRPC server, and <see cref="System.Net.Http.HttpClient"/> instrumentation applies to the whole process.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceProvider"/>.</param>
    public static void UseTracing(this IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptionsMonitor<TelemetryOptions>>().CurrentValue;
        var logger = services.GetRequiredService<ILogger>();

        if (!options.TracingEnabled)
        {
            logger.Information("Tracing is disabled.");

            return;
        }

        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.TracingServiceName,
                    serviceVersion: version,
                    serviceInstanceId: Environment.MachineName
                )
                // The same environment that selects the settings, so traces can be filtered per environment.
                .AddAttributes([new("deployment.environment.name", EnvironmentDataProvider.EnvironmentName)])
            )
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TracingSampleRatio)))
            .AddSource(GridBotTelemetry.SourceName)
            .AddAspNetCoreInstrumentation(instrumentation =>
            {
                // Scrapes and health checks would otherwise dominate the traces.
                instrumentation.Filter = context =>
                {
                    var path = context.Request.Path;

                    return !path.StartsWithSegments("/metrics")
                        && !path.StartsWithSegments("/health")
                        && !path.Value.EndsWith("/CheckHealth", StringComparison.Ordinal);
                };
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(exporter =>
            {
                exporter.Endpoint = new Uri(options.TracingOtlpEndpoint);
                exporter.Protocol = options.TracingOtlpProtocol == TracingOtlpProtocol.HttpProtobuf
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;

                if (!string.IsNullOrWhiteSpace(options.TracingOtlpHeaders))
                    exporter.Headers = options.TracingOtlpHeaders;
            })
            .Build();

        // Lets log lines be matched to their trace.
        Logger.GlobalLogPrefixes.Add(GridBotTelemetry.GetTraceLogPrefix);

        // The process exits without unwinding, so flush any pending spans on the way out.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => _tracerProvider?.Dispose();

        logger.Information(
            "Tracing is enabled, exporting to {0} over {1} with a sample ratio of {2}.",
            options.TracingOtlpEndpoint,
            options.TracingOtlpProtocol,
            options.TracingSampleRatio
        );
    }
}
