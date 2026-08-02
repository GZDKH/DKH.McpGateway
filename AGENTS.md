# AGENTS.md

<!-- BEGIN REQUIRED-READING -->

## Required Reading (MUST read before working)

Before starting any task in this repository, read the shared DKH.AgentRules entrypoint:

1. **[AGENTS.md](../../agents/DKH.AgentRules/AGENTS.md)** — shared Codex entrypoint and on-demand trigger index

Profiles, skills, build gates, contracts, releases, and docs rules are lazy-loaded from `agents/DKH.AgentRules`. Use `../../agents/DKH.AgentRules/rules/codex/triggers.md` to decide what else to open for the current task.

---

<!-- END REQUIRED-READING -->
This file provides guidance to Codex when working in this repository.

> **Baseline rules**: The shared `DKH.AgentRules/AGENTS.md` entrypoint referenced above owns unified GZDKH rules. This file adds gateway-specific context only.

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
- `GrpcEndpointsRegistration.cs` — all gRPC client registrations (100 clients at the recorded baseline)
- `Tools/Common/McpJsonDefaults.cs` — shared JSON serialization options
- `Auth/McpToolSurface.cs` and `McpSurfaceGuard.cs` — attribute-based public/admin tool classification and default-closed authorization

## MCP Capabilities

### Tools (actions AI can perform)

The current assembly exposes **356** `[McpServerTool]` methods across **274** `[McpServerToolType]` files. Counts below are derived from the live attributes; re-count them rather than editing totals from memory.

| Folder | Tools | Service |
|--------|-------|---------|
| Products/ | search, get, list catalogs/categories, category distribution, create/update/delete, stats (7) | ProductCatalogService |
| Catalogs/ | manage catalogs (1) | ProductCatalogService |
| Categories/ | manage categories (1) | ProductCatalogService |
| Tags/ | manage tags (1) | ProductCatalogService |
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
| TelegramClient/ | archive query, media download/export, monitoring, session reads (14; phone/auth credentials excluded) | TelegramClientService |
| DataExchange/ | product catalog, reference, customer, order, review import/export (5) | Multiple |
| Inventory/ | manage stock, query stock, reservations, alerts (4) | InventoryService |
| Cart/ | list/get carts, issue claim code, claim cart (4) | CartService |
| Payment/ | payment and payment-plan reads (4) | PaymentService |
| Subscription/ | plan and user-subscription reads (3) | SubscriptionService |
| Delivery/ | quotes, fulfillment, shipment, dispatch, allocation, cancellation, customs, SLA analytics (14) | DeliveryService |
| DeliveryClaims/ | claim lifecycle and evidence (4) | DeliveryService |
| DeliverySettlements/ | settlement reads and carrier invoice import (3) | DeliveryService |
| Logistics/ | rate cards, surcharges, carrier capabilities, rate calculation (22) | LogisticsService |
| Warehouse/ | warehouse CRUD/status lifecycle (7) | WarehouseService |
| WarehouseZones/ | zone CRUD/status lifecycle (7) | WarehouseService |
| HandlingTasks/ | handling task lifecycle and assignment (7) | WarehouseService |
| IncomingShipments/ | incoming shipment and confirmations (5) | WarehouseService |
| TransferOrders/ | transfer lifecycle and receiving (6) | WarehouseService |
| Procurement/ | sourcing, inspection, POs, receiving, returns, custom orders (32; PII masked) | ProcurementService |
| Customs/ | declarations, packets, duties, restrictions and HS/nomenclature catalogs (44; document bytes excluded) | CustomsService |
| Counterparty/ | identity, media/doc metadata, ACL, verification, relationships and financial views (36; contact channels excluded) | CounterpartyService |
| Staff/ | employees, departments, onboarding, shifts, device presence (16; PII masked) | StaffService |
| Engagement/ | request, template and report lifecycles (17; identities masked) | EngagementService |
| Assistant/ | chat, streaming chat, suggestions, clear session (4; user ID input not echoed) | AssistantService |
| ProductRequest/ | request CRUD, status, restore/delete, translations (12; customer ID remains opaque) | ProductRequestService |
| Broadcast/ | broadcast CRUD, retry and cancellation (7) | BroadcastService |
| Notifications/ | delivery/bulk-job health and failures (5; read-only, contacts excluded) | NotificationService |
| Media/ | assets, signed downloads, attachments, upload sessions and scope registry (8) | MediaService |
| Print/ | printers and print-job routing/query (5) | PrintService |
| **StorefrontPublic/** | **per-tenant public** catalog/category/product search/recommendation tools (5) | StorefrontService + ProductCatalogService + SearchService |

#### Public storefront namespace

`StorefrontPublicToolAttribute`, not a name prefix, is the source of truth for the public surface. A storefront-scoped key can discover/invoke only those five tools, must pass `IStorefrontMcpGate` (`McpEnabled`), and every catalog read is constrained to the key's storefront. All unannotated or unknown tools are admin by default. Admin access requires both an `ApiKeyScope.Mcp` key and a bearer principal satisfying the MCP role policy; `ApiKeyAuthMiddleware` admits only MCP or Storefront scopes.

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
| ProductCatalogService | 5003 | 15 clients (query, CRUD, specs, attrs, variants, data exchange) |
| ReferenceService | 5004 | 15 clients (localized management reads, canonical CRUD writes, data exchange) |
| TelegramBotService | 5001 | 4 clients (management, scheduling, notifications, auth) |
| TelegramClientService | 5015 | 3 clients (archive, monitoring, sessions) |
| OrderService | 5007 | 2 clients (CRUD, data exchange) |
| StorefrontService | 5009 | 6 clients (CRUD, branding, catalogs, channels, domains, features) |
| CustomerService | 5010 | 1 client (data exchange) |
| ReviewService | 5011 | 3 clients (CRUD, query, data exchange) |
| ApiManagementService | 5012 | 2 clients (key validation, usage recording) |
| InventoryService | 5014 | 4 clients (stock query, management, reservations, alerts) |
| CartService | 5008 | 2 clients (cart CRUD/query, claim) |
| PaymentService | 5028 | 1 client (read-only payment/plan queries) |
| SubscriptionService | 5024 | 1 client (read-only plan/subscription queries) |
| DeliveryService | 5027 | 8 clients |
| SearchService | 5017 | 1 client |
| LogisticsService | 5019 | 4 clients |
| WarehouseService | 5021 | 5 clients |
| CustomsService | 5022 | 7 clients |
| CounterpartyService | 5020 | 4 clients |
| StaffService | 5031 | 1 client |
| EngagementService | 5032 | 1 client |
| AssistantService | 5023 | 1 client (conversational chat, suggestions, session clearing) |
| ProductRequestService | 5018 | 1 client |
| BroadcastService | 5016 | 1 client |
| NotificationService | 5002 | 1 client |
| MediaService | 5026 | 4 clients |
| ProcurementService | 5030 | 1 client |
| PrintService | 5029 | 1 client |

`GrpcEndpointsRegistration.cs` and the base `Platform:Grpc:Endpoints` configuration must remain one-for-one. At this baseline both contain 100 clients. Use the shared config-sync workflow whenever a client is added or renamed.

## Tool Development Rules

- **Assembly discovery** — every tool container uses `[McpServerToolType]` and every exposed method uses `[McpServerTool]`; a cohesive multi-method container is allowed
- **One resource per method** — resources grouped by domain in `Resources/` folder
- **Surface authorization** — public tools require `[StorefrontPublicTool]`; everything else remains default-closed admin surface
- **Identifiers follow the downstream contract** — prefer human-readable codes when the service supports them; otherwise keep IDs opaque and never echo actor/customer identity unnecessarily
- **No secrets or PII** — mask contacts/employee identities, omit document bytes and auth credentials, and return raw/signed values only where the explicit tool contract requires them
- **JSON responses** — use `McpJsonDefaults.Options` for consistent serialization
- **Translations as JSON** — accept translations as `[{"lang":"en","name":"..."}]` string parameter

## Configuration

- No database (stateless gateway)
- gRPC endpoints: `Platform:Grpc:Endpoints` section in appsettings
- Docker port: 5013
- Streamable HTTP is mapped at `/mcp`; legacy trusted-client compatibility routes remain `/mcp/sse` and `/mcp/message`
- `Mcp:PublicEndpoint` is the exact canonical OAuth resource for every HTTP transport route
- `Platform:Network:KnownProxies` must list only the real reverse proxy so forwarded scheme/host validation is trustworthy
- `Platform:Auth:Keycloak:AdditionalAudiences` includes the canonical MCP resource audience

## External Dependencies

- DKH.Platform.* (Logging, Grpc.Client, Http)
- ModelContextProtocol C# SDK (1.4.1)
- gRPC contracts from downstream services (via NuGet)

<!-- CLAUDE-BASELINE-SHA256: e01bb284198f2d9f7a2edbf33e8db703de4ceefcaeffd5937ed83c8e419aaa2a -->
