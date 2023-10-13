# Base Image: net6.0
FROM mcr.microsoft.com/dotnet/aspnet:6.0

ARG IMAGE_TAG=

WORKDIR /opt/grid
COPY ./deploy/${IMAGE_TAG}/ /opt/grid/

COPY ./ssl/global-root-ca.crt /usr/local/share/ca-certificates/global-root-ca.crt
RUN chmod 644 /usr/local/share/ca-certificates/global-root-ca.crt && update-ca-certificates

RUN mkdir /opt/grid/logs
RUN mkdir /opt/grid/scripts

CMD ["dotnet", "/opt/grid/Grid.Bot.dll"]
