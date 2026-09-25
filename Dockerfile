# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AshaNandanvan.slnx ./
COPY src/AshaNandanvan.Domain/AshaNandanvan.Domain.csproj src/AshaNandanvan.Domain/
COPY src/AshaNandanvan.Application/AshaNandanvan.Application.csproj src/AshaNandanvan.Application/
COPY src/AshaNandanvan.Infrastructure/AshaNandanvan.Infrastructure.csproj src/AshaNandanvan.Infrastructure/
COPY src/AshaNandanvan.Web/AshaNandanvan.Web.csproj src/AshaNandanvan.Web/
RUN dotnet restore src/AshaNandanvan.Web/AshaNandanvan.Web.csproj
COPY src/ src/
RUN dotnet publish src/AshaNandanvan.Web/AshaNandanvan.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS debug
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl unzip \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /src
ENV DOTNET_USE_POLLING_FILE_WATCHER=1 \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads \
    && chown $APP_UID:$APP_UID /app/wwwroot/uploads
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENTRYPOINT ["dotnet", "AshaNandanvan.Web.dll"]
