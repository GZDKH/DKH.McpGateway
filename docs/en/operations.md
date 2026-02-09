# Operations

## Running locally

```bash
# HTTP transport (default)
dotnet run --project DKH.McpGateway.Api

# stdio transport
dotnet run --project DKH.McpGateway.Api -- --stdio
```

## Docker

```bash
docker compose up -d
```

## Health check

The MCP server exposes SSE endpoint at `http://localhost:5013/sse`.

## Logs

- Local: `logs/dkh-mcp-gateway-*.log`
- Docker: `/app/logs/mcp-gateway-*.log` (mounted to `dkh-mcp-gateway-api-logs` volume)
