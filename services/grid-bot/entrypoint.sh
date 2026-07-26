#!/usr/bin/sh

# If PRE_SCRIPTS_DIR defined, then loop thru it and run each script (chmod +x) before starting the main script
if [ -n "$PRE_SCRIPTS_DIR" ]; then
  for script in "$PRE_SCRIPTS_DIR"/*.sh; do
    if [ -f "$script" ]; then
      echo "Running pre-script: $script"

      dos2unix "$script" 2>/dev/null || true
      chmod +x "$script"

      # Source the script to run it in the current shell context
      . "$script"
    fi
  done
fi

dotnet /all/Grid.Bot.dll
