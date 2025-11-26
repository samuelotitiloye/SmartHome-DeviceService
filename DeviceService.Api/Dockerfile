# ============================
#  BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# copy only csproj (layer caching)
COPY SmartHome.sln ./
COPY DeviceService.Api/DeviceService.Api.csproj DeviceService.Api/
COPY DeviceService.Application/DeviceService.Application.csproj DeviceService.Application/
COPY DeviceService.Domain/DeviceService.Domain.csproj DeviceService.Domain/
COPY DeviceService.Infrastructure/DeviceService.Infrastructure.csproj DeviceService.Infrastructure/

RUN dotnet restore DeviceService.Api/DeviceService.Api.csproj

# copy full sln
COPY . .

#WORKDIR /src/DeviceService.Api
RUN dotnet publish DeviceService.Api/DeviceService.Api.csproj -c Release -o /app/publish

# ============================
# RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

#Install tini (init system)
RUN apk add --no-cache tini bash

# Add wait-for-it 
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

#create non-root user
RUN adduser -D appuser
RUN chown -R appuser /app

USER appuser

# Copy publish output
COPY --from=build /app/publish . 

# expose port
EXPOSE 8080

#Health check endpoint
HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
 CMD wget -q0- http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["/sbin/tini", "--"]
CMD ["bash", "/wait-for-it.sh", "postgres:5432", "--", "dotnet", "DeviceService.Api.dll"]