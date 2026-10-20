# Single-node Loki using local filesystem storage. The service is exposed only
# through Consul; do not publish unauthenticated log ingestion through Traefik.
job "loki" {
  type        = "service"
  datacenters = ["*"]
  namespace   = "o11y"

  group "loki" {
    count = 1

    update {
      auto_revert = true
    }

    network {
      port "http" {
        static = 3100
        to     = 3100
      }
    }

    task "loki" {
      shutdown_delay = "5s"
      kill_timeout   = "30s"

      service {
        name = "loki"
        tags = [
          "prometheus",
          "traefik",
          "traefik.enable=true",
          "traefik.http.routers.loki.rule=Host(`loki.wck02.vmminfra.dev`)",
          "traefik.http.routers.loki.entrypoints=http,https"
        ]
        port = "http"

        check {
          type     = "http"
          path     = "/ready"
          interval = "10s"
          timeout  = "5s"
        }
      }

      resources {
        cpu    = 1000
        memory = 2048
      }

      driver = "docker"
      user   = "root"

      config {
        image = "grafana/loki:3.5.5"
        args  = ["-config.file=/local/loki.yaml"]
        ports = ["http"]

        volumes = ["/_/data/loki:/loki"]
      }

      template {
        data = <<EOH
auth_enabled: false

server:
  http_listen_address: 0.0.0.0
  http_listen_port: 3100

common:
  path_prefix: /loki
  ring:
    instance_addr: 127.0.0.1
    kvstore:
      store: inmemory
  replication_factor: 1

schema_config:
  configs:
    - from: "2024-01-01"
      store: tsdb
      object_store: filesystem
      schema: v13
      index:
        prefix: index_
        period: 24h

storage_config:
  tsdb_shipper:
    active_index_directory: /loki/tsdb-index
    cache_location: /loki/tsdb-cache
  filesystem:
    directory: /loki/chunks

compactor:
  working_directory: /loki/compactor
  compaction_interval: 10m
  retention_enabled: true
  delete_request_store: filesystem
  retention_delete_delay: 2h

limits_config:
  allow_structured_metadata: true
  retention_period: 168h

analytics:
  reporting_enabled: false
EOH

        destination = "local/loki.yaml"
        change_mode = "restart"
      }
    }
  }
}
