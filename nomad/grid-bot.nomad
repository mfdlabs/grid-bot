#  Copyright 2023 MFDLABS. All rights reserved.

job "{{{NOMAD_JOB_NAME}}}" {
  datacenters = ["*"]

  vault {
    policies = ["vault_secret_grid_settings_read_write"]
  }

  meta {
    environment = "{{{NOMAD_ENVIRONMENT}}}"
  }

  group "grid-bot" {
    count = 1

    network {
      mode = "host"

      port "metrics" {
        static = 8101
      }

      port "grpc" {
        static = 5000
      }
    }

    task "runner" {
      driver = "docker"

      config {
        image        = "{{{IMAGE_NAME}}}:{{{IMAGE_TAG}}}"
        network_mode = "host"

        # /var/run/docker.sock:/var/run/docker.sock
        volumes = [
          "/var/run/docker.sock:/var/run/docker.sock",
          "/tmp/.X11-unix:/tmp/.X11-unix",
          "/opt/grid/scripts:/opt/grid/scripts",
          "/_/_logs/grid-bot/{{{NOMAD_ENVIRONMENT}}}:/tmp/mfdlabs/logs"
        ]

        hostname = "{{{NOMAD_JOB_NAME}}}.service.consul"
      }

      resources {
        memory = {{{NOMAD_MEMORY}}}
        cpu = {{{NOMAD_CPU}}}
      }

      template {
        data        = <<EOF

DISPLAY=:1

# CONSUL
VAULT_ADDR="http://vault.service.consul:8200"
VAULT_TOKEN="{{ with secret "grid-bot-settings/grid-bot-vault" }}{{ .Data.data.vault_token }}{{ end }}"
ENVIRONMENT="{{ env "NOMAD_META_environment" }}"
EOF
        destination = "secrets/grid-bot.env"
        env         = true
      }

      service {
        name = "{{{NOMAD_JOB_NAME}}}"
        port = "metrics"

        tags = [
          "{{{NOMAD_ENVIRONMENT}}}"
        ]

        check {
          type     = "http"
          path     = "/metrics"
          interval = "2s"
          timeout  = "2s"
        }
      }
    }
  }
}
