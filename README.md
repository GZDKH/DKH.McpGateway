# DKH.McpGateway

MCP (Model Context Protocol) gateway that exposes DKH ecosystem functionality to AI clients such as Claude Desktop, Claude Code, and Cursor.

## Documentation

- [Architecture Docs (EN)](https://github.com/GZDKH/DKH.Architecture/blob/main/en/services/gateway/mcp-gateway-index.md)
- [Architecture Docs (RU)](https://github.com/GZDKH/DKH.Architecture/blob/main/ru/services/gateway/mcp-gateway-index.md)
- [Local Docs](./docs/README.md)

## Quick Start

```bash
dotnet restore
dotnet build -c Release
dotnet run --project DKH.McpGateway.Api
```
