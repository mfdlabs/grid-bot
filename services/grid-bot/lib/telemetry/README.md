# Grid.Bot Telemetry

Tracing for the Grid.Bot service, built on OpenTelemetry. Metrics live in
[`../metrics`](../metrics/README.md).

Tracing is **off by default**. While it is off, starting a span returns `null`
and costs effectively nothing.

## Enabling tracing

Settings bind from the `Telemetry` section, or from environment variables
using the property name. They can also come from Vault at the `telemetry` path.

| Setting | Default | Description |
| --- | --- | --- |
| `TracingEnabled` | `false` | Turns on trace export. |
| `TracingOtlpEndpoint` | `http://localhost:4317` | Where traces are sent. For `HttpProtobuf` this must include the signal path, such as `http://localhost:4318/v1/traces`. |
| `TracingOtlpProtocol` | `Grpc` | `Grpc` or `HttpProtobuf`. |
| `TracingOtlpHeaders` | | Comma separated `key=value` headers sent on each export, for example an auth token. Keep this in Vault. |
| `TracingSampleRatio` | `1.0` | Fraction of traces to record, from 0 to 1. Child spans follow their parent. |
| `TracingServiceName` | `grid-bot` | The `service.name` reported with traces. |

For example, to export to a local collector:

```
TracingEnabled=true
TracingOtlpEndpoint=http://localhost:4317
```

Spans are flushed when the process exits.

Every trace is tagged with the `deployment.environment.name` resource attribute,
taken from the same environment name that selects the settings (`ENVIRONMENT`,
or `NOMAD_META_ENVIRONMENT`, defaulting to `development`). Filter on it in
Jaeger, for example with the tag `deployment.environment.name=staging`, to split
traces by environment.

## What is traced

`GridBotTelemetry` defines the `Grid.Bot` `ActivitySource`, and the span and
attribute names.

| Span | Kind | Covers |
| --- | --- | --- |
| `command.execute` | internal | A text command, from dispatch to completion. |
| `interaction.execute` | internal | An interaction, from dispatch until the executed event fires. |
| `script.execute` | internal | A script execution request. |
| `script.log` | internal | Logging the script. |
| `render.execute` | internal | A render request. |
| `grid.job.acquire` | internal | Acquiring a Grid job, including waiting for a ready instance. |
| `grid.soap.execute` | client | Running the job on the Grid server. |
| `discord.respond` | client | Sending the response to Discord. |
| `client_settings.refresh` | internal | A client-settings refresh. It is a root span, as it runs in the background. |

In addition, the standard instrumentation records the web server and gRPC
server requests (`/metrics`, `/health`, and `CheckHealth` are excluded) and
every `HttpClient` call, such as the rbx-thumbnails requests.

Every span ends with a `gridbot.outcome` attribute using the same values as the
metrics `outcome` label. `exception` and `timeout` also mark the span as an
error.

### Attributes

Unlike metric labels, span attributes can carry request-specific values, which
is what makes traces useful for investigating a single request. Spans record the
Discord user, guild, and channel identifiers.

Never record message content, scripts, or exception messages, as they can
contain user input. Failed spans record only the exception type.

## Logs

When tracing is enabled, log lines written inside a trace get a
`[trace_id=<id>]` prefix. Outside of a trace there is no prefix. This lets you
jump from a log line to its trace.

### Exporting logs to Loki

Log export is **off by default**. When enabled, every line written through
`Logger` is also exported over OTLP, so it can be stored in Grafana Loki and
explored on a dashboard. The console and log file output are unchanged.

| Setting | Default | Description |
| --- | --- | --- |
| `LoggingEnabled` | `false` | Turns on log export. |
| `LoggingOtlpEndpoint` | `http://localhost:4317` | Where logs are sent. For `HttpProtobuf` this must include the signal path, which is `<loki>/otlp/v1/logs` for Loki. |
| `LoggingOtlpProtocol` | `Grpc` | `Grpc` or `HttpProtobuf`. Loki accepts `HttpProtobuf` only. |
| `LoggingOtlpHeaders` | | Comma separated `key=value` headers sent on each export, for example an auth token or `X-Scope-OrgID=<tenant>`. Keep this in Vault. |
| `LoggingMinimumLevel` | `Information` | The most verbose level that is exported: `Error`, `Warning`, `Information`, `Debug`, or `Verbose`. |
| `LoggingServiceName` | `grid-bot` | The `service.name` reported with logs. |

For example, to export to a local Loki:

```
LoggingEnabled=true
LoggingOtlpEndpoint=http://localhost:3100/otlp/v1/logs
LoggingOtlpProtocol=HttpProtobuf
```

Start Loki, Prometheus, Jaeger, and Grafana from the repository root, using the
Compose files in the scripts directories:

```sh
docker compose -f services/grid-bot/scripts/loki/docker-compose.yml up -d
docker compose -f services/grid-bot/scripts/prometheus/docker-compose.yml up -d
docker compose -f services/grid-bot/scripts/jaeger/docker-compose.yml up -d
docker compose -f services/grid-bot/scripts/grafana/docker-compose.yml up -d
```

All four files use the same Compose project and network, so Grafana can reach
the data sources by their service names. Data persists in named volumes.
Prometheus scrapes the bot's default metrics address at
`host.docker.internal:8081`; on Linux, the Compose file provides that host
alias. The Grafana UI is at `http://localhost:3000`, Prometheus at
`http://localhost:9090`, and Jaeger at `http://localhost:16686`. These web UIs
are bound to localhost. Jaeger accepts OTLP/gRPC on `localhost:4317` and
OTLP/HTTP on `localhost:4318`.

Configure the bot with these local settings:

```sh
TracingEnabled=true
TracingOtlpEndpoint=http://localhost:4317
TracingOtlpProtocol=Grpc
LoggingEnabled=true
LoggingOtlpEndpoint=http://localhost:3100/otlp/v1/logs
LoggingOtlpProtocol=HttpProtobuf
```

The Grafana provisioning files set up Prometheus, Jaeger, and Loki data sources
and load the dashboards from `dashboards/`. The default Grafana login is
`admin` / `admin`; change the password on first login.

Stop all four services from the repository root:

```sh
docker compose -f services/grid-bot/scripts/grafana/docker-compose.yml down
docker compose -f services/grid-bot/scripts/jaeger/docker-compose.yml down
docker compose -f services/grid-bot/scripts/prometheus/docker-compose.yml down
docker compose -f services/grid-bot/scripts/loki/docker-compose.yml down
```

For the Nomad-hosted Loki service, use
`https://loki.wck02.vmminfra.dev/otlp/v1/logs` as the endpoint.

Each exported record has the resource attributes `service.name` and
`deployment.environment.name`, the level as its severity, and a `logger.name`
attribute with the name of the logger that wrote the line. Use `logger.name` to
group or filter logs. Records written inside a trace also carry its `trace_id`
and `span_id`, which needs tracing to be enabled so that there is a trace to
carry.

Loki indexes the service and environment resource attributes by default. The
level, logger name, and `trace_id` are log attributes and remain structured
metadata, which keeps them queryable without adding index labels. Never turn
user, guild, or channel identifiers into labels.

Logs are batched and flushed when the process exits.

The Logs dashboard links to the Traces dashboard using the `trace_id`
structured metadata field.

## Exemplars

Histogram observations and counter increments made inside a trace carry that
trace's ID as an exemplar, so a latency spike on a panel can link to a trace.
Exemplars are only exposed when Prometheus scrapes using the OpenMetrics format,
and Prometheus must be started with `--enable-feature=exemplar-storage`.

## Adding a span

Start the span from the shared source and give it one of the names in
`GridBotTelemetry.Spans`:

```csharp
using var activity = GridBotTelemetry.Source.StartActivity(GridBotTelemetry.Spans.Script);
```

`activity` is `null` when tracing is disabled, so use `?.` or `SetOutcome`,
which accepts `null`. For a single operation, `GridBotTelemetry.Run` and
`RunAsync` start the span, mark it as an error if the operation throws, and
rethrow.
