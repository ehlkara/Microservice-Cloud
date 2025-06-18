FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MembershipService/MembershipService.csproj", "MembershipService/"]
COPY ["Shared.Utils/Shared.Utils.csproj", "Shared.Utils/"]
RUN dotnet restore "MembershipService/MembershipService.csproj"
COPY . .
WORKDIR "/src/MembershipService"
RUN dotnet build "MembershipService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MembershipService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["MembershipService/appsettings.json", "./"]
COPY ["MembershipService/appsettings.Development.json", "./"]
ENTRYPOINT ["dotnet", "MembershipService.dll"] 