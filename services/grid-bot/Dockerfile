FROM mcr.microsoft.com/dotnet/aspnet:8.0.1-jammy

WORKDIR /all
COPY . /all

COPY ./ssl/global-root-ca.crt /usr/local/share/ca-certificates/global-root-ca.crt
RUN chmod 644 /usr/local/share/ca-certificates/global-root-ca.crt && update-ca-certificates

CMD ["dotnet", "/all/Grid.Bot.dll"]
