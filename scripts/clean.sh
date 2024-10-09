#!/bin/sh

# This will enumerate the workspace and remove obj and bin folders

# The workspace is always one directory up from the script directory
WORKSPACE=$(dirname $(dirname $(realpath $0)))

echo "Cleaning workspace: $WORKSPACE"

# Find all obj and bin folders and remove them recursively and log each removal
find $WORKSPACE -type d -name obj -exec rm -rf {} \; -print
find $WORKSPACE -type d -name bin -exec rm -rf {} \; -print