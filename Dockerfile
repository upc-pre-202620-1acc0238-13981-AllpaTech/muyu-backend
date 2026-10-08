# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Muyu.sln ./
COPY src/BuildingBlocks/Muyu.Shared/Muyu.Shared.csproj src/BuildingBlocks/Muyu.Shared/
COPY src/Host/Muyu.API/Muyu.API.csproj src/Host/Muyu.API/
COPY src/BoundedContexts/IAM/Muyu.Modules.IAM.csproj src/BoundedContexts/IAM/
COPY src/BoundedContexts/ParcelManagement/Muyu.Modules.ParcelManagement.csproj src/BoundedContexts/ParcelManagement/
COPY src/BoundedContexts/Agreements/Muyu.Modules.Agreements.csproj src/BoundedContexts/Agreements/
COPY src/BoundedContexts/Escrow/Muyu.Modules.Escrow.csproj src/BoundedContexts/Escrow/
COPY src/BoundedContexts/Tracking/Muyu.Modules.Tracking.csproj src/BoundedContexts/Tracking/
COPY src/BoundedContexts/ClimateAlerts/Muyu.Modules.ClimateAlerts.csproj src/BoundedContexts/ClimateAlerts/

RUN dotnet restore Muyu.sln

COPY . .
RUN dotnet publish src/Host/Muyu.API/Muyu.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Muyu.API.dll"]
