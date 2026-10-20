namespace Grid.Bot;

using Logging;

/// <summary>
/// The protocol used to export traces and logs over OTLP.
/// </summary>
public enum TracingOtlpProtocol
{
    /// <summary>
    /// OTLP over gRPC, usually on port 4317.
    /// </summary>
    Grpc,

    /// <summary>
    /// OTLP over HTTP with protobuf encoding, usually on port 4318.
    /// </summary>
    HttpProtobuf,
}

/// <summary>
/// Options for tracing and other telemetry.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// The vault path for the telemetry options.
    /// </summary>
    public const string VaultPath = "telemetry";

    /// <summary>
    /// Gets or sets a value indicating whether traces are exported. Tracing is off unless this is enabled.
    /// </summary>
    public bool TracingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the OTLP endpoint traces are exported to.
    /// </summary>
    /// <remarks>
    /// For <see cref="TracingOtlpProtocol.HttpProtobuf"/> this must include the signal path, such as <c>http://localhost:4318/v1/traces</c>.
    /// </remarks>
    public string TracingOtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the protocol used to export traces.
    /// </summary>
    public TracingOtlpProtocol TracingOtlpProtocol { get; set; } = TracingOtlpProtocol.Grpc;

    /// <summary>
    /// Gets or sets the headers sent with each export, as comma separated <c>key=value</c> pairs.
    /// </summary>
    public string TracingOtlpHeaders { get; set; }

    /// <summary>
    /// Gets or sets the fraction of traces to record, from 0 to 1. Child spans follow their parent's decision.
    /// </summary>
    public double TracingSampleRatio { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the service name reported with traces.
    /// </summary>
    public string TracingServiceName { get; set; } = "grid-bot";

    /// <summary>
    /// Gets or sets a value indicating whether logs are exported over OTLP, for example to Grafana Loki. Log export is off unless this is enabled.
    /// </summary>
    public bool LoggingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the OTLP endpoint logs are exported to.
    /// </summary>
    /// <remarks>
    /// For <see cref="TracingOtlpProtocol.HttpProtobuf"/> this must include the signal path, such as <c>http://localhost:3100/otlp/v1/logs</c> for Loki.
    /// </remarks>
    public string LoggingOtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the protocol used to export logs.
    /// </summary>
    public TracingOtlpProtocol LoggingOtlpProtocol { get; set; } = TracingOtlpProtocol.Grpc;

    /// <summary>
    /// Gets or sets the headers sent with each log export, as comma separated <c>key=value</c> pairs.
    /// </summary>
    public string LoggingOtlpHeaders { get; set; }

    /// <summary>
    /// Gets or sets the most verbose level that is exported. Messages more verbose than this are not exported.
    /// </summary>
    public LogLevel LoggingMinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the service name reported with logs.
    /// </summary>
    public string LoggingServiceName { get; set; } = "grid-bot";
}
