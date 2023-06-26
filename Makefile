build-linux:

	$(info Building for Linux)
	@cargo build --release --target x86_64-unknown-linux-gnu

build-linux-dbg:

	$(info Building for Linux)
	@cargo build --target x86_64-unknown-linux-gnu

build-windows:

	$(info Building for Windows)
	@cargo build --release --target x86_64-pc-windows-gnu

build-windows-dbg:

	$(info Building for Windows)
	@cargo build --target x86_64-pc-windows-gnu

build:

ifeq ($(OS),Windows_NT)
	$(info Building for Windows)
	@cargo build --release --target x86_64-pc-windows-gnu
else
	$(info Building for Linux)
	@cargo build --release --target x86_64-unknown-linux-gnu
endif

build-dbg:

ifeq ($(OS),Windows_NT)
	$(info Building for Windows)
	@cargo build --target x86_64-pc-windows-gnu
else
	$(info Building for Linux)
	@cargo build --target x86_64-unknown-linux-gnu
endif
