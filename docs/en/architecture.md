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
│   ├── GrpcEndpointsRegistration.cs  # ~40 gRPC client registrations
│   ├── Tools/
│   │   ├── Common/McpJsonDefaults.cs # Shared JSON serialization options
│   │   ├── Products/                 # 11 tools (search, get, CRUD, stats, analytics)
│   │   ├── Brands/                   # 1 tool (manage brands)
│   │   ├── Categories/               # 1 tool (manage categories)
│   │   ├── Tags/                     # 1 tool (manage tags)
│   │   ├── References/               # 6 tools (list + manage)
│   │   ├── Geography/                # 2 tools (country details, product origin)
│   │   ├── Orders/                   # 4 tools (summary, status, trends, top products)
│   │   ├── Reviews/                  # 3 tools (stats, ranking, summary)
│   │   ├── Storefronts/              # 12 tools (query + manage)
│   │   ├── Telegram/                 # 4 tools (bot, channels, groups, scheduling)
│   │   └── DataExchange/             # 2 tools (product catalog, reference data)
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
| Tools | 47 | Actions AI can perform (search, CRUD, analytics) |
| Resources | 9 | Read-only data endpoints (catalogs, references, storefronts) |
| Prompts | 5 | Analytics prompt templates (catalog health, sales, reviews) |

## Downstream services (via gRPC)

| Service | Port | Protocol | Client count |
| ------- | ---- | -------- | ------------ |
| ProductCatalogService | 5003 | gRPC | 14 clients (query, CRUD, data exchange) |
| ReferenceService | 5004 | gRPC | 12 clients (query, CRUD, data exchange) |
| OrderService | 5007 | gRPC | 1 client |
| StorefrontService | 5009 | gRPC | 6 clients (CRUD, branding, catalogs, channels, domains, features) |
| ReviewService | 5011 | gRPC | 2 clients (query, service) |
| TelegramBotService | 5001 | gRPC | 4 clients (management, scheduling, notifications, auth) |

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
- **Action parameter pattern** — CRUD tools accept `string action` (create/update/delete/list) with `StringComparison.OrdinalIgnoreCase`
- **Code-based identification** — use human-readable codes (SEO names, ISO codes) instead of UUIDs
- **Translations as JSON** — accept translations as `[{"lang":"en","name":"..."}]` string parameter
- **No PII** — never expose customer emails, addresses, phone numbers in responses
- **Consistent JSON** — use `McpJsonDefaults.Options` for all serialization

## Configuration

- gRPC endpoints: `Platform:Grpc:Endpoints` section in appsettings
- Transport: `--stdio` flag or `MCP_TRANSPORT=stdio` env variable for stdio mode
- Docker port: 5013
