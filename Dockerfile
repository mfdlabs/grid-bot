# Base Image: net6.0
FROM mcr.microsoft.com/dotnet/runtime:6.0

ARG IMAGE_TAG=

WORKDIR /opt/grid
COPY ./deploy/${IMAGE_TAG}/ /opt/grid/

RUN mkdir /opt/grid/logs
RUN mkdir /opt/grid/scripts

CMD ["dotnet", "/opt/grid/Grid.Bot.dll"]