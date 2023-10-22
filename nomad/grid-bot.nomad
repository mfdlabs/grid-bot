#  Copyright 2023 MFDLABS. All rights reserved.

job "{{{NOMAD_JOB_NAME}}}" {
  datacenters = ["*"]

  vault {
    policies = ["vault_secret_grid_settings_read_write"]
  }

  group "grid-bot" {
    count = 1

    network {
      mode = "host"

      port "metrics" {}
    }

    task "runner" {
      driver = "docker"

      config {
        image        = "${IMAGE_NAME}:${IMAGE_TAG}"
        network_mode = "host"

        # /var/run/docker.sock:/var/run/docker.sock
        volumes = [
          "/var/run/docker.sock:/var/run/docker.sock",
          "/tmp/.X11-unix:/tmp/.X11-unix",
          "/opt/grid/scripts:/opt/grid/scripts",
          "/_/_logs/grid-bot/{{{NOMAD_ENVIRONMENT}}}:/tmp/mfdlabs/logs"
        ]

        hostname = "grid-bot.nomad.vmminfra.dev"
      }

      resources {
        memory = 1024
        cpu = 2000
      }

      template {
        data        = <<EOF

DISPLAY=:0

IMAGE_NAME="{{{IMAGE_NAME}}}"
IMAGE_TAG="{{{IMAGE_TAG}}}"

# CONSUL
CONSUL_ADDR="http://consul.service.consul:8500"
DEFAULT_LOG_FILE_DIRECTORY="/local/logs"

MetricsPort="{{ env "NOMAD_PORT_metrics" }}"

{{ with secret "grid-bot-settings/{{{NOMAD_ENVIRONMENT}}}" }}
{{ if .Data.data }}
{{ range $key, $value := .Data.data }}
{{ $key }} = "{{ $value }}"
{{ end }}
{{ end }}

{{ end }}
EOF
        destination = "secrets/grid-bot.env"
        env         = true
      }

      service {
        name = "grid-bot"

        tags = [
          "{{{NOMAD_ENVIRONMENT}}}"
        ]
      }
    }
  }
}
