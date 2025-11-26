# ============================
# STAGE 1 — Build
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmartHome.sln ./
COPY DeviceService.Api/DeviceService.Api.csproj DeviceService.Api/
COPY DeviceService.Application/DeviceService.Application.csproj DeviceService.Application/
COPY DeviceService.Domain/DeviceService.Domain.csproj DeviceService.Domain/
COPY DeviceService.Infrastructure/DeviceService.Infrastructure.csproj DeviceService.Infrastructure/

RUN dotnet restore DeviceService.Api/DeviceService.Api.csproj

COPY . .
WORKDIR /src/DeviceService.Api
RUN dotnet publish "DeviceService.Api.csproj" -c Release -o /app/publish

# ============================
# STAGE 2 — Publish
# ============================
FROM build AS publish
RUN dotnet publish "DeviceService.Api.csproj" -c Release -o /app/publish

# ============================
# STAGE 2 — Runtime
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy publish output
COPY --from=publish /app/publish .

# Add wait-for-it
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

EXPOSE 8080

ENTRYPOINT ["dotnet", "DeviceService.Api.dll"]