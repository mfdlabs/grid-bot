# This job incorporates a Prometheus server that will
# automatically collect from all metrics endpoints
# of consul jobs with the prometheus tag

job "prometheus" {
  type        = "service"
  datacenters = ["*"]
  namespace   = "o11y"

  group "prometheus" {
    update {
      auto_revert = true
    }

    network {
      port "http" {
        to     = 9090
      }
    }

    task "prometheus" {

      service {
        name = "prometheus"
        tags = [
          "traefik",
          "traefik.enable=true",
          "traefik.http.routers.prometheus.rule=Host(`prometheus.wck02.vmminfra.dev`)",
          "traefik.http.routers.prometheus.entrypoints=http,https"
        ]

        port = "http"

        check {
          type     = "tcp"
          interval = "10s"
          timeout  = "5s"
        }
      }

      driver = "docker"
      user   = "root"

      config {
        args = [
          "--config.file=/local/prometheus.yml",
          "--enable-feature=exemplar-storage",
          "--storage.tsdb.path=/prometheus",
          "--storage.tsdb.retention.time=15d",
          "--storage.tsdb.wal-compression",
          "--web.console.libraries=/usr/share/prometheus/console_libraries",
          "--web.console.templates=/usr/share/prometheus/consoles",
          "--web.enable-lifecycle",
          "--web.external-url=https://prometheus.wck02.vmminfra.dev"
        ]

        image = "prom/prometheus:latest"
        ports = ["http"]

        volumes = ["/_/data/prometheus:/prometheus", "/_/data/prometheus/rules:/rules:ro"]
      }

      template {
        data = <<EOH
rule_files:
  - /rules/*.yml

global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'nomad_metrics'

    consul_sd_configs:
    - server: '{{ env "CONSUL_HTTP_ADDR" }}'
      services: ['nomad-client', 'nomad']

    relabel_configs:
    - source_labels: ['__meta_consul_tags']
      regex: '(.*)http(.*)'
      action: keep

    scrape_interval: 5s
    metrics_path: /v1/metrics
    params:
      format: ['prometheus']
        
  - job_name: "consul-prometheus-exporters"
    scrape_protocols: ['OpenMetricsText1.0.0', 'PrometheusText0.0.4']
    metrics_path: "/metrics"
    consul_sd_configs:
      - server: '{{ env "CONSUL_HTTP_ADDR" }}'
    relabel_configs:
      - source_labels: ['__meta_consul_tags']
        regex: '^.*,prometheus,.*$'
        action: keep
      - source_labels: ['__meta_consul_node']
        target_label: instance
      - source_labels: ['__meta_consul_service']
        target_label: service
      - source_labels: ['__meta_consul_tags']
        target_label: tags
EOH

        destination = "local/prometheus.yml"
        change_mode = "signal"
        change_signal = "SIGHUP"
      }
    }
  }
}
