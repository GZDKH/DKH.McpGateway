# syntax=docker/dockerfile:1
# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 5013

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# Fix for gRPC protoc segfault on arm64: install system protobuf-compiler
# On amd64, bundled protoc works fine, so skip installation
RUN if [ "$(uname -m)" = "aarch64" ] || [ "$(uname -m)" = "arm64" ]; then \
    echo "==> Installing system protoc for arm64 to fix Grpc.Tools segfault"; \
    apt-get update && \
    apt-get install -y --no-install-recommends protobuf-compiler && \
    rm -rf /var/lib/apt/lists/* && \
    protoc --version; \
    else \
    echo "==> Using bundled Grpc.Tools protoc on $(uname -m)"; \
    fi

WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["project.props", "."]
COPY ["nuget.config", "."]

# Inject GitHub NuGet credentials via dotnet CLI (secure, no layer caching)
RUN --mount=type=secret,id=github_nuget_username \
    --mount=type=secret,id=github_nuget_token \
    if [ ! -s /run/secrets/github_nuget_username ] || [ ! -s /run/secrets/github_nuget_token ]; then \
    echo "Missing GitHub NuGet secrets (GITHUB_NUGET_USERNAME/TOKEN)" && exit 1; \
    fi && \
    dotnet nuget update source github-dotnet-gzdkh \
    --username "$(cat /run/secrets/github_nuget_username)" \
    --password "$(cat /run/secrets/github_nuget_token)" \
    --store-password-in-clear-text \
    --configfile nuget.config

COPY ["DKH.McpGateway.Api/DKH.McpGateway.Api.csproj", "DKH.McpGateway.Api/"]
COPY ["DKH.McpGateway.Application/DKH.McpGateway.Application.csproj", "DKH.McpGateway.Application/"]
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    RUNTIME_ID=$(if [ "$(uname -m)" = "aarch64" ] || [ "$(uname -m)" = "arm64" ]; then echo "linux-arm64"; else echo "linux-x64"; fi) && \
    dotnet restore "./DKH.McpGateway.Api/DKH.McpGateway.Api.csproj" -r $RUNTIME_ID /p:PublishReadyToRun=true
# Preserve authenticated nuget.config before COPY overwrites it
RUN cp nuget.config /tmp/nuget.config.auth
COPY . .
RUN cp /tmp/nuget.config.auth nuget.config

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/DKH.McpGateway.Api"
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    RUNTIME_ID=$(if [ "$(uname -m)" = "aarch64" ] || [ "$(uname -m)" = "arm64" ]; then echo "linux-arm64"; else echo "linux-x64"; fi) && \
    dotnet restore "./DKH.McpGateway.Api.csproj" -r $RUNTIME_ID /p:PublishReadyToRun=true && \
    if [ "$(uname -m)" = "aarch64" ] || [ "$(uname -m)" = "arm64" ]; then \
    Protobuf_ProtocPath=/usr/bin/protoc \
    dotnet publish "./DKH.McpGateway.Api.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish -r $RUNTIME_ID /p:UseAppHost=false /p:PublishReadyToRun=true; \
    else \
    dotnet publish "./DKH.McpGateway.Api.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish -r $RUNTIME_ID /p:UseAppHost=false /p:PublishReadyToRun=true; \
    fi

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DKH.McpGateway.Api.dll"]
