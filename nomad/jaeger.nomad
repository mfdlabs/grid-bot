# This job incorporates a Jaeger v2 all-in-one instance (collector, query and UI)

job "jaeger" {
  type        = "service"
  datacenters = ["*"]
  namespace   = "o11y"

  group "jaeger" {
    update {
      auto_revert = true
    }

    network {
      # The collector ports are static so clients can use a stable address,
      # e.g. http://jaeger-collector.service.consul:4317
      port "otlp_grpc" {
        static = 4317
        to     = 4317
      }

      port "otlp_http" {
        to     = 4318
      }

      port "ui" {
        to = 16686
      }

      port "health" {
        to = 13133
      }
    }

    task "jaeger" {

      # OTLP/gRPC collector endpoint
      service {
        name = "jaeger-collector"
        tags = ["otlp", "grpc"]
        port = "otlp_grpc"

        check {
          type     = "tcp"
          interval = "10s"
          timeout  = "5s"
        }
      }

     	# OTLP/HTTP collector endpoint, exposed through traefik over HTTPS
      service {
        name = "jaeger-collector-http"
        tags = [
          "otlp",
          "http",
          "traefik",
          "traefik.enable=true",
          "traefik.http.routers.jaeger-http.rule=Host(`tracing.wck02.vmminfra.dev`)",
          "traefik.http.routers.jaeger-http.entrypoints=https",
          "traefik.http.routers.jaeger-http.tls=true"
        ]
        port = "otlp_http"

        check {
          type     = "tcp"
          interval = "10s"
          timeout  = "5s"
        }
      }

      # Query UI, exposed through traefik
      service {
        name = "jaeger"
        tags = [
          "traefik",
          "traefik.enable=true",
          "traefik.http.routers.jaeger.rule=Host(`jaeger.wck02.vmminfra.dev`)",
          "traefik.http.routers.jaeger.entrypoints=http,https"
        ]

        port = "ui"

        check {
          type     = "http"
          port     = "health"
          path     = "/status"
          interval = "10s"
          timeout  = "5s"
        }
      }

      resources {
        cpu    = 300
        memory = 1024
      }

      driver = "docker"
      user   = "root"

      # Let the services deregister from Consul before stopping, and give badger time to flush.
      shutdown_delay = "5s"
      kill_timeout   = "30s"

      config {
        image = "jaegertracing/jaeger:2.20.0"
        args  = ["--config", "/local/config.yaml"]
        ports = ["otlp_grpc", "otlp_http", "ui", "health"]

        volumes = ["/_/data/jaeger:/badger"]
      }

      template {
        data = <<EOH
service:
  extensions: [jaeger_storage, jaeger_query, healthcheckv2]
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger_storage_exporter]
  telemetry:
    logs:
      level: info

extensions:
  healthcheckv2:
    use_v2: true
    http:
      endpoint: 0.0.0.0:13133

  jaeger_query:
    storage:
      traces: main
    http:
      endpoint: 0.0.0.0:16686

  jaeger_storage:
    backends:
      main:
        badger:
          directories:
            keys: /badger/keys
            values: /badger/values
          ephemeral: false
          ttl:
            spans: 72h

receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:

exporters:
  jaeger_storage_exporter:
    trace_storage: main
EOH

        destination = "local/config.yaml"
        change_mode = "restart"
      }
    }
  }
}