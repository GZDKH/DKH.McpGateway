# Operations

## Running locally

```bash
# HTTP transport (default, port 5013)
dotnet run --project DKH.McpGateway.Api

# stdio transport (for CLI-based MCP clients)
dotnet run --project DKH.McpGateway.Api -- --stdio

# stdio via environment variable
MCP_TRANSPORT=stdio dotnet run --project DKH.McpGateway.Api
```

## Docker

```bash
# Start with docker compose (from DKH.McpGateway root)
docker compose up -d

# Start as part of DKH.Infrastructure stack
docker compose -f docker-compose.services.yml --profile mcp up -d
```

## Transport modes

| Mode | Activation | Use case |
| ---- | ---------- | -------- |
| HTTP SSE | Default (no flags) | Web-based MCP clients, shared server |
| stdio | `--stdio` flag or `MCP_TRANSPORT=stdio` | CLI clients (Claude Code, Cursor) |

Both modes use the same `Platform.CreateWeb()` entry point with identical gRPC client registration, logging, and DI configuration.

## Configuration

### gRPC endpoints

All downstream service addresses are configured in `Platform:Grpc:Endpoints` section:

```json
{
  "Platform": {
    "Grpc": {
      "Endpoints": {
        "ProductQueryServiceClient": {
          "Url": "http://localhost:5003",
          "TimeoutSeconds": 30
        }
      }
    }
  }
}
```

Environment-specific overrides:

- `appsettings.json` — local development (localhost)
- `appsettings.Docker.json` — Docker environment (service names)

### Environment variables

| Variable | Description | Default |
| -------- | ----------- | ------- |
| `ASPNETCORE_ENVIRONMENT` | Configuration environment | `Development` |
| `MCP_TRANSPORT` | Transport mode (`stdio` or empty) | empty (HTTP) |

## Logs

| Environment | Path |
| ----------- | ---- |
| Local | `logs/dkh-mcp-gateway-*.log` |
| Docker | `/app/logs/mcp-gateway-*.log` (volume: `dkh-mcp-gateway-api-logs`) |

Log configuration is in the `Logging` section of appsettings:

- Console: colored output with timestamps
- File: daily rolling, 50 MB limit, 14 days retention

## Health check

The HTTP transport exposes the SSE endpoint at `http://localhost:5013/sse`.

In Docker, health check uses `curl -f http://localhost:5013/sse` with 30s interval and 3 retries.

## Downstream service dependencies

The gateway requires all downstream services to be running. In Docker, `depends_on` with `condition: service_healthy` ensures proper startup order.

| Service | Port | Required for |
| ------- | ---- | ------------ |
| product-catalog | 5003 | Product, brand, category, tag tools |
| reference-service | 5004 | Reference, geography tools |
| order-service | 5007 | Order tools |
| storefront-service | 5009 | Storefront tools |
| review-service | 5011 | Review tools |
| telegram-bot | 5001 | Telegram tools |
