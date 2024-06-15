IMAGE_TAG ?= $(shell $(CURDIR)/scripts/generate-image-tag.sh)

SOLUTION ?= $(CURDIR)/grid-bot.sln#
BARE_SOLUTION ?= $(CURDIR)/grid-bot-bare.sln

.DEFAULT_GOAL := all

all:
	$(info  make <cmd>)
	$(info )
	$(info commands:)
	$(info )
	$(info build-<debug/release>: build the solution in debug/release mode, fetching dependencies from NuGet)
	$(info build-local-<debug/release>: build the solution in debug/release mode, using local dependencies, you must have the grid-bot-libraries cloned!)
	$(info )

build-debug:

	dotnet build $(BARE_SOLUTION) \
	-c debug \
	-p:IMAGE_TAG=$(IMAGE_TAG)-dev \
	-p:CI=true \
	-consoleLoggerParameters:NoSummary \
	-p:GenerateFullPaths=true

build-release:

	dotnet build $(BARE_SOLUTION) \
	-c release \
	-p:IMAGE_TAG=$(IMAGE_TAG) \
	-p:CI=true \
	-consoleLoggerParameters:NoSummary \
	-p:GenerateFullPaths=true

build-local-debug:

	dotnet build $(SOLUTION) \
	-c debug \
	-p:IMAGE_TAG=$(IMAGE_TAG)-dev \
	-p:CI=true \
	-p:GenerateFullPaths=true \
	-p:LocalBuild=true

build-local-release:

	dotnet build $(SOLUTION) \
	-c release \
	-p:IMAGE_TAG=$(IMAGE_TAG) \
	-p:CI=true \
	-p:GenerateFullPaths=true \
	-p:LocalBuild=true
