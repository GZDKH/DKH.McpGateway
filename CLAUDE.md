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
- **Type**: MCP Gateway (protocol translator, stateless)
- **Transport**: stdio and HTTP (SSE/Streamable HTTP)
- **Port**: 5013 (HTTP transport)

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
- `DKH.McpGateway.Api` — Program.cs, dual transport setup (stdio/HTTP)
- `DKH.McpGateway.Application` — MCP tools, resources, prompts, gRPC registration

**Gateway Pattern:**

```text
MCP Protocol (stdio/HTTP) → Tools/Resources/Prompts → gRPC Clients → Downstream Services
```

**Key files:**
- `ConfigureServices.cs` — `AddMcpGatewayServer()` registers tools, resources, prompts
- `GrpcEndpointsRegistration.cs` — all gRPC client registrations (~40 clients)
- `Tools/Common/McpJsonDefaults.cs` — shared JSON serialization options

## MCP Capabilities

### Tools (actions AI can perform)

| Folder | Tools | Service |
|--------|-------|---------|
| Products/ | search, get, list brands/catalogs/categories, create/update/delete, stats, analytics | ProductCatalogService |
| Brands/ | manage brands | ProductCatalogService |
| Categories/ | manage categories | ProductCatalogService |
| Tags/ | manage tags | ProductCatalogService |
| References/ | list/manage countries, currencies, languages, delivery times, measurements | ReferenceService |
| Geography/ | country details, product origin | ReferenceService |
| Orders/ | search orders | OrderService |
| Reviews/ | search reviews, stats | ReviewService |
| Storefronts/ | list/get/manage storefronts, branding, catalogs, channels, domains, features | StorefrontService |
| Telegram/ | manage bots, channels, manager groups, scheduling | TelegramBotService |
| DataExchange/ | product catalog and reference data import/export | ProductCatalog/Reference |

### Resources (read-only data)

| URI | Description |
|-----|-------------|
| `catalog://catalogs` | All product catalogs |
| `catalog://categories` | Category tree (parameterized by catalog) |
| `catalog://products` | Product details (parameterized by SEO name) |
| `reference://countries` | All countries with ISO codes |
| `reference://countries/details` | Country details by code |
| `reference://currencies` | All currencies with codes and symbols |
| `reference://languages` | All supported languages |
| `storefront://storefronts` | All storefronts |
| `storefront://config` | Storefront config with branding and features |

### Prompts (analytics templates)

| Prompt | Description |
|--------|-------------|
| `analyze_catalog` | Catalog health and recommendations |
| `sales_report` | Sales summary for a period |
| `storefront_audit` | Storefront configuration audit |
| `review_analysis` | Review sentiment and trends |
| `data_quality_check` | Data completeness and quality |

## Downstream Services (via gRPC)

| Service | Port | Clients |
|---------|------|---------|
| ProductCatalogService | 5003 | 14 clients (query, CRUD, data exchange) |
| ReferenceService | 5004 | 12 clients (query, CRUD, data exchange) |
| OrderService | 5007 | 1 client |
| StorefrontService | 5009 | 6 clients (CRUD, branding, catalogs, channels, domains, features) |
| ReviewService | 5011 | 2 clients (query, service) |
| TelegramBotService | 5001 | 4 clients (management, scheduling, notifications, auth) |

## Tool Development Rules

- **One tool per file** — each tool is a static class with `[McpServerToolType]`
- **One resource per method** — resources grouped by domain in `Resources/` folder
- **Code-based identification** — use human-readable codes (not UUIDs) for entity lookup
- **Action parameter pattern** — tools accept `string action` (create/update/delete/list)
- **No PII** — never expose customer emails, addresses, phone numbers
- **JSON responses** — use `McpJsonDefaults.Options` for consistent serialization
- **Translations as JSON** — accept translations as `[{"lang":"en","name":"..."}]` string parameter

## Configuration

- No database (stateless gateway)
- gRPC endpoints: `Platform:Grpc:Endpoints` section in appsettings
- Docker port: 5013

## External Dependencies

- DKH.Platform.* (Logging, Grpc.Client, Http)
- ModelContextProtocol C# SDK (0.8.0-preview.1)
- gRPC contracts from downstream services (via NuGet)
