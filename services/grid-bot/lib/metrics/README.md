# Grid.Bot Metrics

`GridBotMetrics` is the registry of every Prometheus metric the Grid.Bot service
exposes. Metric names, help text, types, and label names are defined here, and
instrumentation code references the definitions rather than creating its own.
Each metric group is in its own file, such as `GridBotMetrics.Commands.cs`.

Dashboards and alerts that use these metrics are in
[`/dashboards`](../../../../dashboards/README.md).

## Naming scheme

- Every metric is named `gridbot_<domain>_<thing>_<unit>`.
- Use lowercase snake_case for names and labels.
- Counters end in `_total`. Durations are in seconds and end in `_seconds`.
- The recovery service uses the `gridbot_recovery_` domain.

Names are checked with `promtool check metrics`, which enforces the Prometheus
naming rules.

## Labels

- Every metric has an `environment` label, such as `staging`, added by the bot
  from its `ENVIRONMENT` setting. It is set once at startup with
  `GridBotMetrics.SetEnvironment`, which has to run before any metric is
  created, and it is how dashboards and alerts tell environments apart rather
  than the scrape target's name. It is not part of the label lists below.
- Labels take only stable, bounded values. Fixed sets are defined as constants
  in `MetricLabelValues` and `MetricOutcomes`, so a caller cannot pass an
  arbitrary string.
- Never put a user, guild, channel, script, file, or other request-specific
  identifier in a label. That belongs in a trace, see
  [`../telemetry`](../telemetry/README.md).
- The HTTP `endpoint` label is the route pattern, or `unknown` when no route
  matched. It is never the raw request path.
- Command names in labels come from registered commands. They are lower-cased
  for text commands, and interactions that are not a registered command are
  labeled `unknown`.

### The `outcome` label

`outcome` is one of the values in `MetricOutcomes`: `success`, `user_error`,
`timeout`, `exception`, `rejected` (refused before running, such as by
maintenance mode, a blacklist, or a flood checker), and `not_found`.

## Metrics

### Commands, interactions, scripts and renders

| Metric | Labels |
| --- | --- |
| `gridbot_command_executions_total` | `command_name`, `outcome` |
| `gridbot_command_duration_seconds` | `command_name`, `outcome` |
| `gridbot_commands_in_flight` | |
| `gridbot_interaction_executions_total` | `interaction_type`, `command_name`, `outcome` |
| `gridbot_interaction_duration_seconds` | `interaction_type`, `command_name`, `outcome` |
| `gridbot_interactions_in_flight` | |
| `gridbot_script_executions_total` | `outcome` |
| `gridbot_script_execution_duration_seconds` | `outcome` |
| `gridbot_script_executions_in_flight` | |
| `gridbot_script_run_duration_seconds` | |
| `gridbot_render_executions_total` | `outcome` |
| `gridbot_render_duration_seconds` | `outcome` |
| `gridbot_renders_in_flight` | |

Text commands are timed around `CommandService.ExecuteAsync`, which runs
synchronously. Interactions run asynchronously, so the interaction service
returns before the command finishes. `gridbot_interaction_duration_seconds` is
recorded when the interaction executed event fires, and measures the whole
command.

`gridbot_script_execution_duration_seconds` is how long the bot took to handle
the request. `gridbot_script_run_duration_seconds` is how long the script
itself ran, as reported by the script runtime.

### Refused requests

| Metric | Labels |
| --- | --- |
| `gridbot_requests_rejected_total` | `source` (`message`, `interaction`), `reason` (`maintenance`, `blacklisted`) |
| `gridbot_maintenance_bypasses_total` | `source` |
| `gridbot_flood_check_rejections_total` | `feature` (`render`, `script`), `scope` (`global`, `per_user`) |
| `gridbot_messages_processed_total` | |

### Scripts

| Metric | Labels |
| --- | --- |
| `gridbot_script_syntax_errors_total` | `context` |
| `gridbot_script_rejected_inputs_total` | `reason` (`no_content`, `unicode`) |
| `gridbot_script_result_errors_total` | `reason` (`non_json_serializable`, `non_ascii`) |
| `gridbot_script_oversized_results_total` | |
| `gridbot_script_results_via_file_total` | |
| `gridbot_script_executions_from_files_total` | |
| `gridbot_script_lua_vm_executions_total` | |
| `gridbot_script_unexpected_exceptions_total` | `exception_type` |
| `gridbot_script_logged_total` | `source` |
| `gridbot_script_logged_duplicates_total` | |
| `gridbot_script_logged_hashes` | |

### Renders

| Metric | Labels |
| --- | --- |
| `gridbot_render_backend_total` | `backend` (`rbx_thumbnails`, `grid_server`) |
| `gridbot_render_username_lookups_total` | |
| `gridbot_render_invalid_inputs_total` | `reason` (`invalid_id`, `banned_user`) |
| `gridbot_render_thumbnails_rendered_total` | `command_type` |
| `gridbot_render_rbx_thumbnails_statuses_total` | `status` |
| `gridbot_render_rbx_thumbnails_errors_total` | `state` |
| `gridbot_render_rbx_thumbnails_fetches_total` | `command_type` |
| `gridbot_render_rbx_thumbnails_blocked_id_fetches_total` | |
| `gridbot_render_rbx_thumbnails_rollout_percent` | |
| `gridbot_render_legacy_rejections_total` | `reason` |
| `gridbot_render_legacy_errors_total` | |
| `gridbot_render_blocked_ids` | |
| `gridbot_render_local_cache_size` | |

### Grid, client settings and Discord

| Metric | Labels |
| --- | --- |
| `gridbot_grid_job_acquire_duration_seconds` | `outcome` |
| `gridbot_grid_job_rejections_total` | `reason` |
| `gridbot_grid_jobs_closed_total` | `recycle` |
| `gridbot_grid_active_jobs`, `gridbot_grid_instances`, `gridbot_grid_ready_instances` | |
| `gridbot_client_settings_refresh_duration_seconds` | `outcome` |
| `gridbot_client_settings_refreshes_total`, `_reads_total`, `_writes_total` | `application` (reads of every application are `all`) |
| `gridbot_discord_gateway_latency_seconds` | `shard_id` |
| `gridbot_discord_gateway_disconnects_total` | `shard_id` |
| `gridbot_discord_log_messages_total` | `severity` |
| `gridbot_discord_serializer_errors_total` | |
| `gridbot_discord_webhook_alerts_total`, `_alerts_with_attachments_total` | |
| `gridbot_backtrace_uploads_total` | |
| `gridbot_admin_role_changes_total` | `change` |

The Grid job metrics are recorded by `JobManager`, so they cover both script
executions and renders. The active-job and instance gauges are read when
Prometheus scrapes.

### Web server, gRPC and build

| Metric | Labels |
| --- | --- |
| `gridbot_http_server_requests_total` | `method`, `endpoint` |
| `gridbot_http_server_responses_total` | `method`, `endpoint`, `status_code` |
| `gridbot_http_server_request_duration_seconds` | `method`, `endpoint` |
| `gridbot_http_server_requests_in_flight` | |
| `gridbot_grpc_requests_total` | `method` (`check_health`, `set_status`) |
| `gridbot_build_info` | `version`, `git_hash`, `git_branch`, `build_timestamp`, `session_id` |

### Recovery service

`gridbot_recovery_messages_processed_total`,
`gridbot_recovery_interactions_processed_total` (`interaction_type`), and
`gridbot_recovery_discord_log_messages_total` (`severity`).

### Histogram buckets

The duration histograms use the explicit buckets in `MetricBuckets`. Histogram
observations and counter increments made inside a trace carry that trace's ID
as an exemplar.

## Migrating from the old names

The previous metric names were removed. Metrics that were replaced by an
`outcome` label were removed rather than renamed, and labels that held user or
request identifiers were dropped.

| Old name | New |
| --- | --- |
| `grid_bot_build_info` | `gridbot_build_info` |
| `bot_messages_processed_total` | `gridbot_messages_processed_total` |
| `bot_commands_processed_total`, `bot_commands_failed_total` | `gridbot_command_executions_total` (`outcome`) |
| `bot_command_processing_time_seconds` | `gridbot_command_duration_seconds` |
| `bot_interactions_processed_total`, `bot_interactions_failed_total` | `gridbot_interaction_executions_total` (`outcome`) |
| `bot_interaction_processing_time_seconds` | `gridbot_interaction_duration_seconds` (the old one only timed dispatch) |
| `bot_messages_failed_due_to_maintenance_total`, `bot_interactions_failed_due_to_maintenance_total` | `gridbot_requests_rejected_total{reason="maintenance"}` |
| `bot_blacklisted_user_attempted_messages_total`, `bot_blacklisted_user_attempted_interactions_total` | `gridbot_requests_rejected_total{reason="blacklisted"}` |
| `bot_users_bypassed_maintenance_total` and the `bot_messages_`/`bot_interactions_` variants | `gridbot_maintenance_bypasses_total` |
| `bot_discord_log_messages_total` (`log_severity`) | `gridbot_discord_log_messages_total` (`severity`) |
| `bot_discord_serializer_errors_total` | `gridbot_discord_serializer_errors_total` |
| `script_executions_by_user_total`, `script_executions_success_total`, `script_executions_failed_due_to_lua_error_total`, `script_executions_timed_out_total` | `gridbot_script_executions_total` (`outcome`) |
| `script_execution_average_execution_time_seconds` | `gridbot_script_run_duration_seconds` |
| `script_executions_blocked_by_global_flood_checker_total`, `script_executions_blocked_by_per_user_flood_checker_total` | `gridbot_flood_check_rejections_total{feature="script"}` |
| `script_executions_with_syntax_errors_total` | `gridbot_script_syntax_errors_total` |
| `script_executions_with_no_content_total`, `script_executions_with_unicode_total` | `gridbot_script_rejected_inputs_total` |
| `script_executions_from_files_total` | `gridbot_script_executions_from_files_total` |
| `script_executions_using_lua_vm_total` | `gridbot_script_lua_vm_executions_total` |
| `script_executions_with_non_json_serializable_results_total`, `script_executions_with_non_ascii_results_total` | `gridbot_script_result_errors_total` |
| `script_executions_with_results_exceeding_max_size_total` | `gridbot_script_oversized_results_total` |
| `script_executions_with_results_via_file_total` | `gridbot_script_results_via_file_total` |
| `script_executions_with_unexpected_exceptions_total` | `gridbot_script_unexpected_exceptions_total` |
| `script_logging_script_hashes_total` | `gridbot_script_logged_hashes` |
| `script_logging_scripts_logged_total` | `gridbot_script_logged_total` |
| `script_logging_existing_scripts_logged_total` | `gridbot_script_logged_duplicates_total` |
| `renders_total`, `renders_with_errors_total` | `gridbot_render_executions_total` (`outcome`) |
| `renders_blocked_by_global_flood_checker_total`, `renders_blocked_by_per_user_flood_checker_total` | `gridbot_flood_check_rejections_total{feature="render"}` |
| `renders_via_username_total` | `gridbot_render_username_lookups_total` |
| `renders_with_invalid_ids_total`, `renders_against_banned_users_total` | `gridbot_render_invalid_inputs_total` |
| `renders_with_rbx_thumbnails_errors_total` | `gridbot_render_rbx_thumbnails_errors_total` |
| `avatar_thumbnails_via_rbx_thumbnails_total`, `avatar_thumbnails_via_grid_server_total` | `gridbot_render_backend_total` |
| `avatar_thumbnails_rendered_total` | `gridbot_render_thumbnails_rendered_total` |
| `avatar_thumbnails_rbx_thumbnails_status_total` | `gridbot_render_rbx_thumbnails_statuses_total` |
| `avatar_thumbnails_rbx_thumbnails_fetch_total` | `gridbot_render_rbx_thumbnails_fetches_total` |
| `avatar_thumbnails_rbx_thumbnails_fetch_blacklisted_id_used_total` | `gridbot_render_rbx_thumbnails_blocked_id_fetches_total` |
| `avatar_thumbnails_rbx_thumbnails_rollout_percent` | `gridbot_render_rbx_thumbnails_rollout_percent` |
| `avatar_thumbnails_legacy_rejected_total`, `avatar_thumbnails_legacy_error_total` | `gridbot_render_legacy_rejections_total`, `gridbot_render_legacy_errors_total` |
| `avatar_thumbnails_ids_not_to_use_total`, `avatar_thumbnails_local_cache_size` | `gridbot_render_blocked_ids`, `gridbot_render_local_cache_size` |
| `admin_users_*_total` | `gridbot_admin_role_changes_total` (`change`) |
| `backtrace_exception_upload_total` | `gridbot_backtrace_uploads_total` |
| `discord_webhook_alert_total`, `discord_webhook_alerts_with_attachments_total` | `gridbot_discord_webhook_alerts_total`, `gridbot_discord_webhook_alerts_with_attachments_total` |
| `rbx_client_settings_{refresh,write,read}_total` | `gridbot_client_settings_{refreshes,writes,reads}_total` |
| `grpc_health_check_requests_total`, `grpc_set_status_requests_total` | `gridbot_grpc_requests_total` (`method`) |
| `http_server_requests_total` | `gridbot_http_server_requests_total` |
| `http_server_response_total` | `gridbot_http_server_responses_total` |
| `http_server_request_duration_seconds` | `gridbot_http_server_request_duration_seconds` |
| `http_server_concurrent_requests_total` | `gridbot_http_server_requests_in_flight` |
| `grid_messages_processed_total`, `grid_interactions_processed_total`, `grid_discord_log_messages_total` (recovery) | `gridbot_recovery_*` |

Changes beyond renaming:

- The user, guild, channel, place, file, size, dimension and script hash labels
  were removed. Use traces to look at a single user or request.
- The HTTP `endpoint` label was the controller and action name, or the raw
  request path for routes without a controller. It is now the route pattern.
- Client-settings reads of every application were previously recorded with the
  wrong number of labels and would have thrown. They are now labeled `all`.

## Series that start at zero

A series that is only created by its first increment is first scraped with a
value of 1. Prometheus has no earlier sample to compare it with, so
`increase()` and `rate()` report 0 for that first event, and a panel can show 0
while the counter reads 1. This hides the first occurrence of rare events such
as a timeout, and every event on a quiet environment.

To avoid that, series are created with a value of zero at startup wherever the
label values are known:

- `GridBotMetrics.InitializeSeries` creates every metric and every series whose
  labels are a fixed set, such as every `outcome` and every rejection reason.
- Each registered command's `gridbot_command_executions_total` and
  `gridbot_interaction_executions_total` series are created when the commands
  are registered.
- Job rejection reasons, thumbnail states and command types come from their
  enums, and a shard's disconnect series is created when its latency is first
  reported.

Series whose label values are not known up front still have the gap for their
first event: `application` on the client-settings counters, `exception_type`,
the HTTP `endpoint` and `status_code`, and the per-command duration histograms,
which would otherwise be thousands of series.

When adding a metric with a fixed label set, add it to `InitializeSeries`.

## Not covered

Port allocation (the allocator lives in the shared `lib/grid` libraries, which
do not reference Prometheus) and Discord REST rate-limit hits.

## Tracing

See [`../telemetry`](../telemetry/README.md).
