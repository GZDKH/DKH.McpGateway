# Architecture

DKH.McpGateway is an MCP (Model Context Protocol) server that exposes GZDKH platform capabilities to AI clients.

## Layers

```
DKH.McpGateway.Api/           # Host, transport configuration (stdio/HTTP)
DKH.McpGateway.Application/   # MCP tools, gRPC endpoint registration
```

**No Infrastructure layer** — MCP SDK injects gRPC clients directly into tool method parameters via DI.

## Transport

- **HTTP SSE** (default): `http://localhost:5013/sse` — for web-based MCP clients
- **stdio**: `--stdio` flag or `MCP_TRANSPORT=stdio` env — for CLI-based MCP clients

## Downstream Services

| Service | Port | Protocol |
|---------|------|----------|
| ProductCatalogService | 5003 | gRPC |
| ReferenceService | 5004 | gRPC |
| OrderService | 5007 | gRPC |
| StorefrontService | 5009 | gRPC |
| ReviewService | 5011 | gRPC |
| TelegramBotService | 5001 | gRPC |
