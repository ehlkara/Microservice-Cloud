FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["InvoiceService/InvoiceService.csproj", "InvoiceService/"]
COPY ["Shared.Utils/Shared.Utils.csproj", "Shared.Utils/"]
RUN dotnet restore "InvoiceService/InvoiceService.csproj"
COPY . .
WORKDIR "/src/InvoiceService"
RUN dotnet build "InvoiceService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InvoiceService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["InvoiceService/appsettings.json", "./"]
COPY ["InvoiceService/appsettings.Development.json", "./"]
ENTRYPOINT ["dotnet", "InvoiceService.dll"] 