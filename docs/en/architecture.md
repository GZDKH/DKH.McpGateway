# Architecture

DKH.McpGateway is an MCP (Model Context Protocol) server that exposes GZDKH platform capabilities to AI clients (Claude Desktop, Claude Code, Cursor, etc.). It translates MCP protocol requests into gRPC calls to downstream backend services.

## Design decisions

- **Stateless gateway** — no database, no local state. All data comes from downstream services via gRPC.
- **2-layer structure** — `Api` (host/transport) + `Application` (tools/resources/prompts/gRPC registration). No Infrastructure layer needed because MCP SDK injects gRPC clients directly into tool method parameters via DI.
- **Dual transport** — HTTP SSE (for web-based clients) and stdio (for CLI-based clients like Claude Code). Unified `Platform.CreateWeb()` entry point with conditional transport selection.

## Project structure

```text
DKH.McpGateway/
├── DKH.McpGateway.Api/
│   ├── Program.cs                    # Host, transport selection (stdio/HTTP)
│   ├── appsettings.json              # Local gRPC endpoints (localhost)
│   ├── appsettings.Docker.json       # Docker gRPC endpoints (service names)
│   └── Dockerfile
│
├── DKH.McpGateway.Application/
│   ├── ConfigureServices.cs          # AddMcpGatewayServer() — tools, resources, prompts
│   ├── GrpcEndpointsRegistration.cs  # 51 gRPC client registrations
│   ├── Tools/
│   │   ├── Common/McpJsonDefaults.cs # Shared JSON serialization options
│   │   ├── Products/                 # 9 tools (search, get, manage, list, stats, analytics)
│   │   ├── Brands/                   # 1 tool (manage brands)
│   │   ├── Catalogs/                 # 1 tool (manage catalogs)
│   │   ├── Categories/               # 1 tool (manage categories)
│   │   ├── Tags/                     # 1 tool (manage tags)
│   │   ├── Manufacturers/            # 1 tool (manage manufacturers)
│   │   ├── PackageTypes/             # 1 tool (manage packages)
│   │   ├── Specifications/           # 3 tools (groups, attributes, options)
│   │   ├── ProductAttributes/        # 3 tools (groups, attributes, options)
│   │   ├── Variants/                 # 2 tools (variant attributes, values)
│   │   ├── References/               # 13 tools (list + manage)
│   │   ├── Geography/                # 2 tools (country details, product origin)
│   │   ├── Orders/                   # 4 tools (summary, status, trends, top products)
│   │   ├── Reviews/                  # 3 tools (stats, ranking, summary)
│   │   ├── Storefronts/              # 11 tools (query + manage)
│   │   ├── Telegram/                 # 4 tools (bot, channels, groups, scheduling)
│   │   ├── Inventory/                # 4 tools (query stock, manage, reservations, alerts)
│   │   └── DataExchange/             # 5 tools (product catalog, reference, order, customer, review)
│   ├── Resources/                    # 3 resource classes (9 URI endpoints)
│   └── Prompts/                      # 5 analytics prompt templates
│
├── docker-compose.yml
├── docker-compose.override.yml
└── docs/
```

## MCP capabilities

The gateway exposes three types of MCP primitives:

| Primitive | Count | Purpose |
| --------- | ----- | ------- |
| Tools | 69 | Actions AI can perform (search, CRUD, analytics, data exchange, inventory) |
| Resources | 9 | Read-only data endpoints (catalogs, references, storefronts) |
| Prompts | 5 | Analytics prompt templates (catalog health, sales, reviews) |

## Downstream services (via gRPC)

| Service | Port | Protocol | Client count |
| ------- | ---- | -------- | ------------ |
| ProductCatalogService | 5003 | gRPC | 17 clients (query, CRUD, data exchange) |
| ReferenceService | 5004 | gRPC | 12 clients (query, CRUD, data exchange) |
| OrderService | 5007 | gRPC | 2 clients (CRUD, data exchange) |
| CustomerService | 5010 | gRPC | 1 client (data exchange) |
| StorefrontService | 5009 | gRPC | 6 clients (CRUD, branding, catalogs, channels, domains, features) |
| ReviewService | 5011 | gRPC | 3 clients (query, CRUD, data exchange) |
| TelegramBotService | 5001 | gRPC | 4 clients (management, scheduling, notifications, auth) |
| ApiManagementService | 5012 | gRPC | 2 clients (API key query, usage tracking) |
| InventoryService | 5014 | gRPC | 4 clients (stock query, management, reservations, alerts) |

## Request flow

```text
AI Client (Claude Desktop / Claude Code / Cursor)
  │
  ▼
MCP Protocol (stdio or HTTP SSE)
  │
  ▼
DKH.McpGateway.Application — Tool / Resource / Prompt handler
  │
  ▼
gRPC Client (from DI) → Downstream Backend Service
  │
  ▼
JSON response → MCP protocol → AI Client
```

## Key design patterns

- **One tool per file** — each tool is a static class with `[McpServerToolType]` attribute
- **Action parameter pattern** — CRUD tools accept `string action` (create/update/delete/get/list) with `StringComparison.OrdinalIgnoreCase`
- **Code-based identification** — use human-readable codes (SEO names, ISO codes) instead of UUIDs
- **Translations as JSON** — accept translations as `[{"lang":"en","name":"..."}]` string parameter
- **No PII** — never expose customer emails, addresses, phone numbers in responses
- **Consistent JSON** — use `McpJsonDefaults.Options` for all serialization

## Configuration

- gRPC endpoints: `Platform:Grpc:Endpoints` section in appsettings
- Transport: `--stdio` flag or `MCP_TRANSPORT=stdio` env variable for stdio mode
- Docker port: 5013

*Last updated: February 2026*
