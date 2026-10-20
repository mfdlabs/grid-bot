FROM mcr.microsoft.com/dotnet/aspnet:8.0.1-jammy

RUN apt-get update && apt-get install -y --no-install-recommends \
    bash \
    jq \
    curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /all
COPY . /all

RUN chmod +x /all/entrypoint.sh

ENTRYPOINT ["/all/entrypoint.sh"]
