FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /source
COPY *.csproj .
RUN dotnet restore
COPY ExperimentConfigSidecar .
COPY appsettings.json .
RUN dotnet publish --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./ExperimentConfigSidecar"]