# CLAUDE.md

## Required Reading (MUST read before working)

Before starting any task in this repository, you MUST read these files from DKH.Architecture:

1. **[AGENTS.md](https://github.com/GZDKH/DKH.Architecture/blob/main/AGENTS.md)** — baseline rules for all repos
2. **[agents-dotnet.md](https://github.com/GZDKH/DKH.Architecture/blob/main/docs/agents-dotnet.md)** — .NET specific rules
3. **[github-workflow.md](https://github.com/GZDKH/DKH.Architecture/blob/main/docs/github-workflow.md)** — GitHub Issues & Project Board

---

This file provides guidance to Claude Code when working in this repository.

> **Baseline rules**: See `AGENTS.md` for unified GZDKH rules (SOLID, DDD, commits, code style, quality guardrails). This file adds service-specific context only.

## Project Overview

DKH.McpGateway is an MCP (Model Context Protocol) gateway that exposes DKH ecosystem functionality to AI clients (Claude Desktop, Claude Code, Cursor, etc.). It translates MCP protocol requests into gRPC calls to downstream backend services.

- **Framework**: .NET 10.0
- **Type**: MCP Gateway (protocol translator)
- **Transport**: stdio and HTTP (SSE/Streamable HTTP)

## Build Commands

```bash
dotnet restore
dotnet build -c Release
dotnet test
dotnet format --verify-no-changes
dotnet run --project DKH.McpGateway.Api
```

## Architecture

**Project Structure:**
- `DKH.McpGateway.Api` — Program.cs, MCP server setup, DI wiring
- `DKH.McpGateway.Application` — MCP tools, resources, prompts, gRPC client wrappers, mappers

**Gateway Pattern:**
MCP Protocol (stdio/HTTP) → MCP Tools/Resources → gRPC Clients → Downstream Services

## MCP Capabilities

**Tools** (actions AI can perform):
- Product search, listing, management
- Reference data queries and management
- Order and review analytics
- Storefront management
- Data import/export
- Telegram bot management

**Resources** (data AI can read):
- Product catalogs
- Reference data (countries, currencies, etc.)

**Prompts** (templates for common workflows):
- Analytics report generation

## Downstream Services (via gRPC)

| Service | Port | Purpose |
|---------|------|---------|
| ProductCatalogService | 5003 | Product management |
| ReferenceService | 5004 | Reference data |
| OrderService | 5007 | Order analytics |
| CartService | 5008 | Cart operations |
| ReviewService | 5011 | Review analytics |
| ApiManagementService | 5012 | API key validation |

## Authentication

- API key validation via DKH.ApiManagementService gRPC
- Key format: `dkh_mcp_{random32}`
- Scope: `mcp`

## Configuration

- Port: `5013` (HTTP)
- No database (stateless gateway)
- gRPC endpoints under `Platform:Grpc:Endpoints`

## External Dependencies

- DKH.Platform.* packages (Logging, Grpc.Client)
- ModelContextProtocol C# SDK
- gRPC contracts from downstream services (via NuGet)
