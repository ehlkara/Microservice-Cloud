FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY SnapshotCoordinator/SnapshotCoordinator.csproj SnapshotCoordinator/
RUN dotnet restore SnapshotCoordinator/SnapshotCoordinator.csproj
COPY SnapshotCoordinator/. SnapshotCoordinator/
WORKDIR /src/SnapshotCoordinator
RUN dotnet build SnapshotCoordinator.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish SnapshotCoordinator.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY SnapshotCoordinator/appsettings.json appsettings.json
COPY SnapshotCoordinator/appsettings.Development.json appsettings.Development.json
ENTRYPOINT ["dotnet", "SnapshotCoordinator.dll"] 