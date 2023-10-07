#!/bin/sh

# Generates a docker tag like this:
# yyyy.mm.dd-hh.mm.ss-commit_hash (e.g. 2019.01.01-12.00.00-abcdef)

# Get the current date and time
DATE=$(date +"%Y.%m.%d-%H.%M.%S")

# Get the current commit hash
# Check if the current directory has a .git folder
if [ -d .git ]; then
    # Check if there is any revision (rev-parse returns a "fatal: Needed a single revision" error if there is no revision)
    if [ -n "$(git rev-parse HEAD)" ]; then
        # Get the commit hash
        COMMIT_HASH=$(git rev-parse --short HEAD)
    else
        # If there is no revision, then we are not in a git repository
        COMMIT_HASH="unknown"
    fi
else
    # If there is no .git folder, then we are not in a git repository
    COMMIT_HASH="unknown"
fi

# Print the tag
echo "$DATE-$COMMIT_HASH"
