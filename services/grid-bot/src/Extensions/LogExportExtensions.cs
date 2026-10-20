namespace Grid.Bot.Extensions;

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;

using Logging;

using ILogger = Logging.ILogger;
using LogLevel = Logging.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;
using MsLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

/// <summary>
/// Extension methods for exporting logs over OTLP.
/// </summary>
public static class LogExportExtensions
{
    // The attribute that holds the name of the logger that wrote the line, used as the "group" when filtering.
    internal const string LoggerNameAttribute = "logger.name";

    private static readonly ConcurrentDictionary<string, MsLogger> _loggers = new();

    private static MsLoggerFactory _loggerFactory;
    private static LogLevel _minimumLevel;

    /// <summary>
    /// Starts exporting logs when <see cref="TelemetryOptions.LoggingEnabled"/> is set.
    /// </summary>
    /// <remarks>
    /// Every log line written through <see cref="Logging.Logger"/> is exported, with the trace and span of the
    /// current <see cref="System.Diagnostics.Activity"/> so that logs can be matched to their trace.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceProvider"/>.</param>
    public static void UseLogExport(this IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptionsMonitor<TelemetryOptions>>().CurrentValue;
        var logger = services.GetRequiredService<ILogger>();

        if (!options.LoggingEnabled)
        {
            logger.Information("Log export is disabled.");

            return;
        }

        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        _minimumLevel = options.LoggingMinimumLevel;
        _loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(MsLogLevel.Trace)
            .AddOpenTelemetry(otel =>
            {
                otel.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: options.LoggingServiceName,
                        serviceVersion: version,
                        serviceInstanceId: Environment.MachineName
                    )
                    // The same environment that selects the settings, so logs can be filtered per environment.
                    .AddAttributes([new("deployment.environment.name", EnvironmentDataProvider.EnvironmentName)])
                );

                otel.IncludeFormattedMessage = true;

                otel.AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(options.LoggingOtlpEndpoint);
                    exporter.Protocol = options.LoggingOtlpProtocol == TracingOtlpProtocol.HttpProtobuf
                        ? OtlpExportProtocol.HttpProtobuf
                        : OtlpExportProtocol.Grpc;

                    if (!string.IsNullOrWhiteSpace(options.LoggingOtlpHeaders))
                        exporter.Headers = options.LoggingOtlpHeaders;
                });
            }));

        Logger.LogWritten += Export;

        // The process exits without unwinding, so flush any pending logs on the way out.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Logger.LogWritten -= Export;
            _loggerFactory?.Dispose();
        };

        logger.Information(
            "Log export is enabled, exporting to {0} over {1} at level {2} and above.",
            options.LoggingOtlpEndpoint,
            options.LoggingOtlpProtocol,
            options.LoggingMinimumLevel
        );
    }

    private static void Export(LogLevel level, string loggerName, string message)
    {
        if (level == LogLevel.None || level > _minimumLevel) return;

        var logger = _loggers.GetOrAdd(loggerName, name => _loggerFactory.CreateLogger(name));

        var state = new List<KeyValuePair<string, object>> { new(LoggerNameAttribute, loggerName) };

        logger.Log(
            ToMsLogLevel(level),
            default,
            state,
            null,
            (_, _) => message
        );
    }

    private static MsLogLevel ToMsLogLevel(LogLevel level)
        => level switch
        {
            LogLevel.Error => MsLogLevel.Error,
            LogLevel.Warning => MsLogLevel.Warning,
            LogLevel.Information => MsLogLevel.Information,
            LogLevel.Debug => MsLogLevel.Debug,
            _ => MsLogLevel.Trace,
        };
}
