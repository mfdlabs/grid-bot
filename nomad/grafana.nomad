# This job incorporates a Grafana server

job "grafana" {
  type        = "service"
  datacenters = ["*"]
  namespace   = "o11y"

  group "grafana" {
    update {
      auto_revert = true
    }

    network {
      port "http" {
        to     = 3000
      }
    }

    task "grafana" {

      service {
        name = "grafana"
        tags = [
          "traefik",
          "traefik.enable=true",
          "traefik.http.routers.grafana.rule=Host(`grafana.wck02.vmminfra.dev`)",
          "traefik.http.routers.grafana.entrypoints=http,https"
        ]

        port = "http"

        check {
          type     = "tcp"
          interval = "10s"
          timeout  = "5s"
        }
      }
      
      resources {
      	memory = 1024
      }

      driver = "docker"
      user   = "root"

      config {
        image = "grafana/grafana:latest"
        ports = ["http"]

        volumes = ["/_/data/grafana:/var/lib/grafana", "/_/data/grafana/dashboards:/var/lib/grafana/dashboards"]
      }

      env {
        GF_USERS_ALLOW_SIGN_UP = "false"
        GF_SERVER_DOMAIN       = "grafana.wck02.vmminfra.dev"
        GF_SERVER_ROOT_URL     = "https://grafana.wck02.vmminfra.dev"
        GF_PATHS_PROVISIONING  = "/local/provisioning"
      }

      template {
        data = <<EOH
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: https://prometheus.wck02.vmminfra.dev
    isDefault: true
    jsonData:
      tlsSkipVerify: true
      exemplarTraceIdDestinations:
        - name: trace_id
          datasourceUid: jaeger

  - name: Jaeger
    uid: jaeger
    type: jaeger
    access: proxy
    url: https://jaeger.wck02.vmminfra.dev
    jsonData:
      tlsSkipVerify: true
        
  - name: Loki
    uid: loki
    type: loki
    access: proxy
    url: https://loki.wck02.vmminfra.dev
    jsonData:
      tlsSkipVerify: true
EOH

        destination = "local/provisioning/datasources/datasource.yml"
        change_mode = "restart"
      }

      template {
        data = <<EOH
apiVersion: 1

providers:
  - name: 'Default'
    orgId: 1
    folder: ''
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards
EOH

        destination = "local/provisioning/dashboards/dashboards.yml"
        change_mode = "noop"
      }
    }
  }
}
