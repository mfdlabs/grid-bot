FROM mcr.microsoft.com/dotnet/aspnet:8.0.1-jammy

RUN apt-get update && apt-get install -y --no-install-recommends \
    bash \
    jq \
    curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /all
COPY . /all

COPY ./ssl/global-root-ca.crt /usr/local/share/ca-certificates/global-root-ca.crt
RUN chmod 644 /usr/local/share/ca-certificates/global-root-ca.crt && update-ca-certificates

RUN chmod +x /all/entrypoint.sh

ENTRYPOINT ["/all/entrypoint.sh"]
