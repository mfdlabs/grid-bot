# Base Image: net8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0.1-jammy

WORKDIR /opt/grid
COPY . /opt/grid/

CMD ["dotnet", "/opt/grid/Grid.Bot.Recovery.dll"]
