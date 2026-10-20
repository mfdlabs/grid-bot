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

For Grafana Loki, add a derived field to the data source with the matcher
`trace_id=(\w+)` that links to the Tempo or Jaeger data source.

## Exemplars

Histogram observations made inside a trace carry that trace's ID as an
exemplar, so a latency spike on a panel can link to a trace. Exemplars are only
exposed when Prometheus scrapes using the OpenMetrics format, and Prometheus
must be started with `--enable-feature=exemplar-storage`.

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
