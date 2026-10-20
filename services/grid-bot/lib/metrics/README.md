# Grid.Bot Metrics

`GridBotMetrics` is the shared registry of Prometheus metric definitions used by
the Grid.Bot service. Metric names, help text, types, and label names belong in
this project; instrumentation code should reference the shared definitions.
Existing metric names and label sets are retained during the initial
centralization except where an existing name was registered with incompatible
label sets.

## Conventions for new metrics

- Prefix new metrics with `gridbot_<domain>_`.
- Use lowercase snake_case names and labels.
- Counters end in `_total`; durations use seconds and end in `_seconds`.
- Use stable, bounded label values only. Suitable labels include a known command
  name, interaction type, outcome, or HTTP method/status code.
- Do not put user, guild, channel, script, file, or other request-specific
  identifiers in labels. Use logs or traces for per-entity investigation.
- Normalize HTTP endpoint labels to route templates; never use an unbounded
  request path as a label.

These conventions apply to new and migrated metrics. Existing names remain
unchanged for compatibility unless their current definitions cannot coexist.

## Initial inventory findings

Definitions were centralized from the bot's command and interaction handlers,
script/render commands, HTTP middleware, gRPC server, client-settings,
administration, Discord logging/alerts, and Backtrace integration.

The current label sets still include high-cardinality or potentially
high-cardinality values. These are intentionally recorded here for later
migration; centralization does not change their behavior:

- User identifiers or names: `user_id`, `user_name_or_id`,
  `interaction_user_id`, `message_user_id`.
- Other entity/request identifiers: `message_channel_id`,
  `message_guild_id`, `place_id`, `script_hash`.
- Unbounded values: `file_name`, `file_size`, `total_size`, `dimension_x`,
  `dimension_y`, `exception_type`, and HTTP `endpoint` when it falls back to
  the raw request path.
- `command_name` is generally bounded by registered commands, but unknown
  interaction types currently fall back to the interaction ID and should be
  normalized before using it as a label.

The message and interaction handlers also used the same metric name for
maintenance bypass counts with incompatible label schemas. The registry gives
these metrics distinct `bot_messages_...` and `bot_interactions_...` names so
both can be registered without a name/label collision.

## Better recording

The metrics below follow the conventions above and are recorded alongside the
legacy metrics, so existing dashboards keep working until they are migrated.

### The `outcome` label

`outcome` is one of the values in `MetricOutcomes`: `success`, `user_error`,
`timeout`, `exception`, `rejected` (refused before running, such as by
maintenance mode, a blacklist, or a flood checker), and `not_found`.

### Metrics

| Metric | Labels | Replaces |
| --- | --- | --- |
| `gridbot_command_executions_total` | `command_name`, `outcome` | `bot_commands_processed_total`, `bot_commands_failed_total` |
| `gridbot_command_duration_seconds` | `command_name`, `outcome` | `bot_command_processing_time_seconds` |
| `gridbot_commands_in_flight` | | |
| `gridbot_interaction_executions_total` | `interaction_type`, `command_name`, `outcome` | `bot_interactions_processed_total`, `bot_interactions_failed_total` |
| `gridbot_interaction_duration_seconds` | `interaction_type`, `command_name`, `outcome` | `bot_interaction_processing_time_seconds` |
| `gridbot_interactions_in_flight` | | |
| `gridbot_script_executions_total` | `outcome` | `script_executions_by_user_total`, `script_executions_success_total` |
| `gridbot_script_execution_duration_seconds` | `outcome` | `script_execution_average_execution_time_seconds` |
| `gridbot_script_executions_in_flight` | | |
| `gridbot_render_executions_total` | `outcome` | `renders_total`, `renders_with_errors_total` |
| `gridbot_render_duration_seconds` | `outcome` | |
| `gridbot_renders_in_flight` | | |
| `gridbot_grid_job_acquire_duration_seconds` | `outcome` | |
| `gridbot_grid_job_rejections_total` | `reason` | |
| `gridbot_grid_jobs_closed_total` | `recycle` | |
| `gridbot_grid_active_jobs`, `gridbot_grid_instances`, `gridbot_grid_ready_instances` | | |
| `gridbot_client_settings_refresh_duration_seconds` | `outcome` | |
| `gridbot_discord_gateway_latency_seconds` | `shard_id` | |
| `gridbot_discord_gateway_disconnects_total` | `shard_id` | |

All new duration metrics are histograms with the explicit buckets in
`MetricBuckets`.

Notes:

- Command names in the new metrics are lower-cased, because command aliases are
  matched case-insensitively and each casing would otherwise be its own series.
  Interaction types that are not a registered command are labeled `unknown`.
- Text commands are timed around `CommandService.ExecuteAsync`, which runs
  synchronously. Interactions default to asynchronous execution, so the
  interaction service returns before the command finishes. The legacy
  `bot_interaction_processing_time_seconds` therefore only measures dispatch;
  `gridbot_interaction_duration_seconds` is recorded when the interaction
  executed event fires and measures the real duration.
- The Grid job metrics are recorded by `JobManager`, so they cover both script
  executions and renders. The active-job and instance gauges are read at scrape
  time.

### Legacy metrics

The legacy metrics are unchanged and are still emitted. Dashboards should move
to the metrics above, after which the legacy ones can be removed. In
particular `script_executions_by_user_total` has an unbounded `user_id` label
and should not be relied on.

Not yet covered: port allocation (the allocator lives in the shared `lib/grid`
libraries, which do not reference Prometheus) and Discord REST rate-limit hits.

## Tracing

Request-specific detail that must not be a metric label belongs in traces. See
[`../telemetry`](../telemetry/README.md). Histogram observations made inside a
trace are recorded with that trace's ID as an exemplar.
