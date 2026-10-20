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

## Linking latency to traces

Latency panels show exemplars, which are points that link to a trace. This
needs three things:

1. **Prometheus** must be started with `--enable-feature=exemplar-storage` and
   scrape the bot using the OpenMetrics format, which is negotiated
   automatically when it is supported.
2. **A Jaeger data source**, and the Prometheus data source pointed at it:

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

Check and test the rules with `promtool`:

```
promtool check rules alerts/grid-bot.rules.yml
promtool test rules alerts/grid-bot.rules.test.yml
```

The tests are unit tests over synthetic series. They cover the high error rate
(including that low traffic and rejected requests do not alert), a single
environment alerting, a gauge that stays above zero, and a refresh failure that
recovers.
