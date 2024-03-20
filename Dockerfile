# Base Image: net8.0
FROM alpine:latest

ARG IMAGE_TAG=

WORKDIR /opt/grid
COPY ./deploy/${IMAGE_TAG}/ /opt/grid/

COPY ./ssl/global-root-ca.crt /usr/local/share/ca-certificates/global-root-ca.crt
RUN cat /usr/local/share/ca-certificates/global-root-ca.crt >> /etc/ssl/certs/ca-certificates.crt

RUN mkdir /opt/grid/logs
RUN mkdir /opt/grid/scripts

CMD ["dotnet", "/opt/grid/Grid.Bot.dll"]
