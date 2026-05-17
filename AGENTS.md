# AGENTS.md

<!-- BEGIN REQUIRED-READING -->

## Required Reading (MUST read before working)

Before starting any task in this repository, read the shared DKH.AgentRules entrypoint:

1. **[AGENTS.md](../../agents/DKH.AgentRules/AGENTS.md)** — shared Codex entrypoint and on-demand trigger index

Profiles, skills, build gates, contracts, releases, and docs rules are lazy-loaded from `agents/DKH.AgentRules`. Use `../../agents/DKH.AgentRules/rules/codex/triggers.md` to decide what else to open for the current task.

---

<!-- END REQUIRED-READING -->
This file provides guidance to Codex when working in this repository.

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
- `GrpcEndpointsRegistration.cs` — all gRPC client registrations (51 clients)
- `Tools/Common/McpJsonDefaults.cs` — shared JSON serialization options

## MCP Capabilities

### Tools (actions AI can perform)

| Folder | Tools | Service |
|--------|-------|---------|
| Products/ | search, get, list brands/catalogs/categories, create/update/delete, stats, analytics (9) | ProductCatalogService |
| Brands/ | manage brands (1) | ProductCatalogService |
| Catalogs/ | manage catalogs (1) | ProductCatalogService |
| Categories/ | manage categories (1) | ProductCatalogService |
| Tags/ | manage tags (1) | ProductCatalogService |
| Manufacturers/ | manage manufacturers (1) | ProductCatalogService |
| PackageTypes/ | manage package types (1) | ProductCatalogService |
| Specifications/ | manage spec groups, attributes, options (3) | ProductCatalogService |
| ProductAttributes/ | manage product attr groups, attributes, options (3) | ProductCatalogService |
| Variants/ | manage variant attributes, values (2) | ProductCatalogService |
| References/ | list/manage countries, currencies, languages, delivery times, measurements (13) | ReferenceService |
| Geography/ | country details, product origin (2) | ReferenceService |
| Orders/ | order summary, status distribution, trends, top sellers (4) | OrderService |
| Reviews/ | review stats, summary, product ranking (3) | ReviewService |
| Storefronts/ | list/get/manage storefronts, branding, catalogs, channels, domains, features (11) | StorefrontService |
| Telegram/ | manage bots, channels, manager groups, scheduling (4) | TelegramBotService |
| DataExchange/ | product catalog, reference, customer, order, review import/export (5) | Multiple |
| Inventory/ | manage stock, query stock, reservations, alerts (4) | InventoryService |

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
| ProductCatalogService | 5003 | 17 clients (query, CRUD, specs, attrs, variants, data exchange) |
| ReferenceService | 5004 | 12 clients (query, CRUD, data exchange) |
| TelegramBotService | 5001 | 4 clients (management, scheduling, notifications, auth) |
| OrderService | 5007 | 2 clients (CRUD, data exchange) |
| StorefrontService | 5009 | 6 clients (CRUD, branding, catalogs, channels, domains, features) |
| CustomerService | 5010 | 1 client (data exchange) |
| ReviewService | 5011 | 3 clients (CRUD, query, data exchange) |
| ApiManagementService | 5012 | 2 clients (key validation, usage recording) |
| InventoryService | 5014 | 4 clients (stock query, management, reservations, alerts) |

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
