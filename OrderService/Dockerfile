FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["OrderService/OrderService.csproj", "OrderService/"]
COPY ["Shared.Utils/Shared.Utils.csproj", "Shared.Utils/"]
RUN dotnet restore "OrderService/OrderService.csproj"
COPY . .
WORKDIR "/src/OrderService"
RUN dotnet build "OrderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["OrderService/appsettings.json", "./"]
COPY ["OrderService/appsettings.Development.json", "./"]
ENTRYPOINT ["dotnet", "OrderService.dll"] 