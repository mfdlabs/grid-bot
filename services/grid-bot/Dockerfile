FROM mcr.microsoft.com/dotnet/aspnet:8.0.1-jammy

WORKDIR /opt/grid
COPY . /opt/grid/

RUN apt-get -y update && \
    apt-get install -y dotnet-sdk-8.0
RUN dotnet tool install --global dotnet-dump

COPY ./ssl/global-root-ca.crt /usr/local/share/ca-certificates/global-root-ca.crt
RUN chmod 644 /usr/local/share/ca-certificates/global-root-ca.crt && update-ca-certificates

RUN mkdir /opt/grid/logs
RUN mkdir /opt/grid/scripts

CMD ["dotnet", "/opt/grid/Grid.Bot.dll"]
