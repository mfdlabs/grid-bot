# Grid Bot dashboards and alerts

Grafana dashboards and Prometheus alerting rules for the Grid Bot. The metrics
they use are documented in
[`services/grid-bot/lib/metrics`](../services/grid-bot/lib/metrics/README.md).

| File | Contents |
| --- | --- |
| `grid-bot-overview.json` | Status, traffic, error ratio, latency, in-flight work, refused requests, Grid, and .NET runtime. Start here. |
| `grid-bot-commands.json` | Text commands and interactions per command and outcome, latency, access control, and the Discord client. |
| `grid-bot-scripts-renders.json` | Script execution and avatar rendering, including rbx-thumbnails. |
| `grid-bot-grid.json` | Grid jobs and instances, client settings, and the Discord gateway per shard. |
| `grid-bot-http.json` | The web server per endpoint, and the gRPC API. |
| `grid-bot-alerts.json` | Firing and pending alerts per environment, how long they have been active, alert history, and which dashboard to use for each alert. |
| `grid-bot-traces.json` | Traces of command and interaction executions per environment, failed and timed out executions, and a single trace by ID. Needs Grafana 13 and a Jaeger data source, see [Traces dashboard](#traces-dashboard). |
| `grid-bot-logs.json` | OTLP logs per environment, filterable by level, logger name, trace ID, and text. Needs a Loki data source, see [Logs dashboard](#logs-dashboard). |
| `alerts/grid-bot.rules.yml` | Alerting rules. |
| `alerts/grid-bot.rules.test.yml` | Unit tests for the alerting rules. |

## Using the dashboards

The dashboards are plain Grafana JSON, so they are provisioned from files.
Copy the `*.json` files into the directory your Grafana dashboard provider
reads, such as `/_/data/grafana/dashboards`. Grafana picks up changes within
the provider's update interval.

They need no hard coded identifiers:

- **Data source:** the `Data source` variable lists your Prometheus data
  sources.
- **Loki data source:** the Logs dashboard lists Grafana Loki data sources.
  Provision one with UID `loki` and URL `https://loki.wck02.vmminfra.dev`,
  or point it at your own Loki endpoint. The local observability Compose setup
  provisions Loki, Prometheus, and Jaeger data sources automatically.
- **Environment:** the `Environment` variable lists the values of the
  `environment` label on `gridbot_build_info`. The bot adds that label to every
  metric it exposes, including the runtime ones, so every query filters on it
  and each environment is viewed on its own. It comes from the bot's own
  `ENVIRONMENT` setting, so it does not depend on how Prometheus discovers or
  names the scrape target, and no relabeling is needed.
- **Navigation:** every dashboard is tagged `grid-bot` and has a dropdown of
  links to the others that keeps the time range and variables.

Restarts are shown as annotations, and the dashboards refresh every 30 seconds.

To change a dashboard, edit it in Grafana and export it with **Export for
sharing externally** turned off, then replace the file here. Keep the `uid`.

## Traces dashboard

`grid-bot-traces.json` uses the Grafana dashboard v2 schema
(`apiVersion: dashboard.grafana.app/v2`), because it shows the single trace
panel only while a trace ID is set. That needs the show/hide rules of dynamic
dashboards, which are on by default in Grafana 13 and have no equivalent in the
classic schema. Grafana provisions it from the same directory as the others, as
long as the file stays in the Kubernetes resource format, so do not replace it
with a classic export. Export it with the **V2 Resource** model instead.

It filters Jaeger by the `deployment.environment.name` resource attribute, using
the dashboard's `Environment` value. Select a trace ID in a table to open the
trace on the dashboard, or enter one in the `Trace ID` variable. Until one is
set, a hint is shown instead of the trace panel.

## Logs dashboard

`grid-bot-logs.json` uses the classic Grafana dashboard schema and a Loki data
source. Its `Environment` variable queries the `deployment_environment_name`
OTLP resource label. The logger filter matches the `logger_name` structured
metadata field, the level filter matches `severity_text`, and the trace filter
matches `trace_id`. Loki normalizes dots in OTLP attribute names to
underscores. Resource attributes such as `service.name` and
`deployment.environment.name` are indexed labels by default; log attributes
such as `severity_text`, `logger.name`, and `trace_id` remain structured
metadata.

Both the Logs and Traces dashboards have a direct link to the other. The trace
ID filter is passed along with the environment and time range. The Logs
dashboard's data source picker requires a Loki data source provisioned in
Grafana; dashboard provisioning does not create data sources. The local
observability Compose setup provisions the Loki data source automatically.

## Linking latency to traces

Latency panels show exemplars, which are points that link to a trace. This
needs three things:

1. **Prometheus** must be started with `--enable-feature=exemplar-storage` and
   scrape the bot using the OpenMetrics format, which is negotiated
   automatically when it is supported.
2. **A Jaeger data source**, and the Prometheus data source pointed at it. Grafana's
   Jaeger data source uses Jaeger's older HTTP API (`/api/services` and
   `/api/traces`), which Jaeger removed in 2.21.0. Against 2.21.0 or later the
   data source test fails with `request failed: 404 Not Found`, so run Jaeger
   **2.20.0 or earlier** for as long as Grafana depends on that API. The Jaeger
   UI itself is not affected.

   ```yaml
   apiVersion: 1

   datasources:
     - name: Prometheus
       type: prometheus
       access: proxy
       url: https://prometheus.wck02.vmminfra.dev
       isDefault: true
       jsonData:
         exemplarTraceIdDestinations:
           - name: trace_id
             datasourceUid: jaeger
     - name: Jaeger
       uid: jaeger
       type: jaeger
       access: proxy
       url: https://jaeger.wck02.vmminfra.dev
     - name: Loki
       uid: loki
       type: loki
       access: proxy
       url: https://loki.wck02.vmminfra.dev
   ```

3. **Tracing enabled** in the bot, see
   [`services/grid-bot/lib/telemetry`](../services/grid-bot/lib/telemetry/README.md).

Traces can be filtered by environment with the tag
`deployment.environment.name`.

## Alerts

Add the rules file to the `rule_files` of your Prometheus configuration. Every
alert is evaluated per `environment`, so an alert for staging does not fire
because of production, and several instances of one environment are combined.

| Alert | Severity | Fires when |
| --- | --- | --- |
| `GridBotHighErrorRate` | warning | More than 5% of executions are exceptions or timeouts for 10 minutes, with enough traffic to mean something. Rejected requests and Lua errors in a user's script do not count. |
| `GridBotCommandLatencyHigh` | warning | The p95 time from dispatch to completion is above 10 seconds for 10 minutes. |
| `GridBotNoReadyGridInstances` | critical | No Grid instance is ready to take a job for 5 minutes. |
| `GridBotGridJobsRejected` | warning | More than 5 Grid jobs were rejected in 10 minutes. |
| `GridBotGatewayDisconnecting` | warning | A shard disconnected more than 3 times in 10 minutes. |
| `GridBotGatewayLatencyHigh` | warning | The worst shard latency is above 1 second for 10 minutes. |
| `GridBotHttp5xxRate` | warning | More than 5% of HTTP responses are 5xx for 10 minutes. |
| `GridBotClientSettingsRefreshFailing` | warning | A client-settings refresh failed in the last 15 minutes. |
| `GridBotInFlightNeverIdle` | warning | An in-flight gauge has not been zero for 30 minutes, which means stuck work or work that is not being counted as finished. |

The thresholds are starting points. Tune them against real traffic.

The rules file also defines a recording rule, `gridbot:executions:rate5m`, with one
series per kind of execution (`type` is `command`, `interaction`, `script` or
`render`). The error rate alert is built on it.

## Writing queries

Do not select several metric families with one `{__name__=~"a|b"}` selector inside
`rate()` or `increase()`. Those functions drop the metric name, so families whose
series have the same labels, such as the script and render executions, collide and
the query fails with `vector cannot contain metrics with the same labelset`.
`promtool` does not report this because it only parses the query. Aggregate each
family on its own, tag it with `label_replace(..., "type", "<family>", "", "")`,
join the branches with `or`, and sum the result, as the overview dashboard does.

Check and test the rules with `promtool`:

```
promtool check rules alerts/grid-bot.rules.yml
promtool test rules alerts/grid-bot.rules.test.yml
```

The tests are unit tests over synthetic series. They cover the high error rate
(including that low traffic and rejected requests do not alert), a single
environment alerting, a gauge that stays above zero, and a refresh failure that
recovers.
