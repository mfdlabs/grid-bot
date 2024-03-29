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

      port "metrics" {
        to = 8101
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

        hostname = "grid-bot.nomad.vmminfra.dev"
      }

      resources {
        memory = 1024
        cpu = 2000
      }

      template {
        data        = <<EOF

DISPLAY=:0

# CONSUL
DEFAULT_LOG_FILE_DIRECTORY="/local/logs"
VAULT_ADDR="http://vault.service.consul:8200"
VAULT_TOKEN="{{ with secret "grid-bot-settings/grid-bot-vault" }}{{ .Data.data.vault_token }}{{ end }}"
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
