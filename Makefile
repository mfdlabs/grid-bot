# `build-dev` is used to build the Go binary but not build the Docker image.
build-dev:
	# build via go
	go build -v -o bin/debug/grid-bot ./...

build-production:
	# build via go
	go build -ldflags "-s -w" -v -o bin/release/grid-bot ./...

build-docker:
	# build via go
	go build -v -o bin/grid-bot ./...
	# build the docker image
	docker build -t grid-bot .

.PHONY: build-dev
