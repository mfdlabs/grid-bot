IMAGE_TAG ?= $(shell ./scripts/generate-image-tag.sh)

SOLUTION ?= $(CURDIR)/grid-bot.sln

build-debug:

	dotnet build $(SOLUTION) \
	-c debug \
	-p:IMAGE_TAG=$(IMAGE_TAG)-dev \
	-p:CI=true \
	-consoleLoggerParameters:NoSummary \
	-p:GenerateFullPaths=true

