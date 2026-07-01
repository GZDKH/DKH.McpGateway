# CLAUDE.md

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
- `GrpcEndpointsRegistration.cs` — all gRPC client registrations (98 clients)
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
| TelegramClient/ | message archive query, media metadata/download/export, chat monitoring, session reads (14; phone/auth credentials omitted) | TelegramClientService |
| DataExchange/ | product catalog, reference, customer, order, review import/export (5) | Multiple |
| Inventory/ | manage stock, query stock, reservations, alerts (4) | InventoryService |
| Cart/ | list/get carts, issue claim code, claim cart (4) | CartService |
| Payment/ | get/list payments, get/list payment plans — read-only (4) | PaymentService |
| Subscription/ | list plans, get/list user subscriptions — read-only (3) | SubscriptionService |
| Delivery/ | quotes, fulfillments, shipments, dispatch, allocation, cancellation, customs, SLA analytics (14) | DeliveryService |
| DeliveryClaims/ | open/add evidence/update status/list claims (4) | DeliveryService |
| DeliverySettlements/ | get/list settlements, import carrier invoice (3) | DeliveryService |
| Logistics/ | rate cards, surcharge rules, carrier capabilities, rate calculation (22) | LogisticsService |
| Warehouse/ | warehouse CRUD/status lifecycle (7) | WarehouseService |
| WarehouseZones/ | warehouse zone CRUD/status lifecycle (7) | WarehouseService |
| HandlingTasks/ | handling task creation, assignment, lifecycle, listing (7) | WarehouseService |
| IncomingShipments/ | incoming shipment queries and factory/warehouse confirmations (5) | WarehouseService |
| TransferOrders/ | transfer order creation, transit, receiving, cancellation, listing (6) | WarehouseService |
| Procurement/ | sourcing, inspections, purchase orders, receiving, returns, custom orders (32; PII masked) | ProcurementService |
| Customs/ | declarations, document packets, duty rules/calculation, trade restrictions, WCO/national HS codes, nomenclature systems (44 incl. legacy duty alias; document bytes omitted) | CustomsService |
| Counterparty/ | counterparty identity, media/documents, ACL, verification, partner relationships, AP balances, financial dashboard (36; PII masked; contact channels excluded) | CounterpartyService |
| Staff/ | employees, departments, onboarding, working shifts, cashier shifts, device presence (16; employee PII masked; heartbeat ingestion excluded) | StaffService |
| Engagement/ | create/assign/start/complete/cancel/get/list requests, list assigned, template CRUD+publish, report lifecycle (17; requester/provider identity masked; profile service excluded) | EngagementService |
| Assistant/ | chat, streaming chat, suggestions, clear session (4; user ID input not echoed) | AssistantService |
| ProductRequest/ | request CRUD, restore/delete, status transitions, translation upsert (12; no contact PII, customerId retained as opaque ID) | ProductRequestService |
| Broadcast/ | create/update/delete/get/list broadcasts, retry and cancel delivery (7; no contact PII, delivery callback excluded) | BroadcastService |
| Notifications/ | delivery health/status and bulk-job status/failure queries (5; read-only; recipient contact values omitted) | NotificationService |
| Media/ | asset storage ref + signed download link, scope registry, attachment list/metadata/reorder/detach, upload-session create (8; actor ids omitted; signed URLs returned) | MediaService |
| Print/ | register printer, list printers, route/get/list print jobs (5; no PII) | PrintService |
| **StorefrontPublic/** | **per-tenant public** — storefront_list_catalogs, storefront_list_brands, storefront_get_category_tree, storefront_get_product, storefront_search_products, storefront_recommend_products (6) | StorefrontService + ProductCatalogService + SearchService |

#### Public storefront namespace (per-tenant, ADR-060)

The `storefront_*` tools are the **public** surface for a single storefront. They require a
`Scope.Storefront` API key bound to one `StorefrontId` (issued via AdminGateway). Each tool gates
on the storefront's `McpEnabled` feature (`IStorefrontMcpGate`), resolves that storefront's catalogs
(`StorefrontScope.ResolveCatalogSeoNamesAsync`), and restricts every downstream read to those
catalogs — a storefront key can never reach another tenant's data. Admin (`Scope.Mcp`) keys lack the
storefront scope and are rejected by these tools; storefront keys lack `mcp:read`/`mcp:write` and
are rejected by the admin tools. `ApiKeyAuthMiddleware` admits only `Scope.Mcp` and `Scope.Storefront`.

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
| TelegramClientService | 5015 | 3 clients (message archive, chat monitoring, session reads) |
| OrderService | 5007 | 2 clients (CRUD, data exchange) |
| StorefrontService | 5009 | 6 clients (CRUD, branding, catalogs, channels, domains, features) |
| CustomerService | 5010 | 1 client (data exchange) |
| ReviewService | 5011 | 3 clients (CRUD, query, data exchange) |
| ApiManagementService | 5012 | 2 clients (key validation, usage recording) |
| InventoryService | 5014 | 4 clients (stock query, management, reservations, alerts) |
| CartService | 5008 | 2 clients (cart CRUD/query, claim) |
| PaymentService | 5028 | 1 client (read-only payments + payment plans) |
| SubscriptionService | 5024 | 1 client (read-only plans + user subscriptions) |
| DeliveryService | 5027 | 8 clients (DeliveryCrud, Dispatch, Claims, Settlements, Analytics, Allocation, Cancellation, CustomsLink) |
| SearchService | 5017 | 1 client (product search + vector recommendations) |
| LogisticsService | 5019 | 4 clients (Quote, RateCard, SurchargeRule, CarrierCapability) |
| WarehouseService | 5021 | 5 clients (WarehouseCrud, WarehouseZoneCrud, HandlingTask, IncomingShipment, TransferOrder) |
| CustomsService | 5022 | 7 clients (Declarations, Duty, TradeRestrictions, DocumentPackets, WCO HS, National HS, NomenclatureSystem) |
| CounterpartyService | 5020 | 4 clients (CounterpartyCrud, PartnerRelationshipCrud, AP Balance, FinancialDashboard) |
| StaffService | 5031 | 1 client (employees, departments, onboarding, shifts, presence) |
| EngagementService | 5032 | 1 client (engagement lifecycle, templates, reports) |
| AssistantService | 5023 | 1 client (conversational chat, suggestions, session clearing) |
| ProductRequestService | 5018 | 1 client (request CRUD + status transitions) |
| BroadcastService | 5016 | 1 client (broadcast CRUD + schedule/cancel/retry) |
| NotificationService | 5002 | 1 client (delivery health/status + bulk-job queries, read-only) |
| MediaService | 5026 | 4 clients (AssetService, AttachmentService, UploadSessionService, ScopeRegistryService) |
| ProcurementService | 5030 | 1 client (Procurement workflow API) |
| PrintService | 5029 | 1 client (Prints — printer registry + print-job queue) |

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
- ModelContextProtocol C# SDK (1.4.0)
- gRPC contracts from downstream services (via NuGet)
