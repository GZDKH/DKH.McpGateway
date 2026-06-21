# MCP Gateway — Service Onboarding Roadmap

Goal: expose **every** DKH ecosystem service through the MCP gateway so AI clients
(Claude Desktop, Cowork, GPT, Cursor) can work with the whole platform.

This document is the execution plan. It is intentionally code-free: the actual tools
must be generated against each service's real gRPC contracts and verified by CI
(see "Execution constraints").

## Current coverage (10 services)

Already integrated (contracts referenced in `Directory.Packages.props`, clients in
`GrpcEndpointsRegistration.cs`):

ProductCatalogService, SearchService, ReferenceService, OrderService, ReviewService,
StorefrontService, InventoryService, TelegramBotService, CustomerService (import/export only),
ApiManagementService (internal auth only).

## Execution constraints (read first)

Each new service requires both of the following, neither of which is available in the
remote web sandbox used to draft this plan:

1. **Real gRPC contracts** — the full `.proto` request/response messages from each
   `DKH.<Service>.Contracts` package. Reverse-engineering from snippets is not reliable.
2. **Build + test verification** — `dotnet` plus access to the private NuGet feed
   (`gitlab.thetea.app`) so the `DKH.<Service>.Contracts` packages restore and the
   solution compiles. The repo enforces `TreatWarningsAsErrors` + analyzers, so code
   must be CI-green, not merely "looks right".

Therefore execution should run in an environment that has the private feed + `dotnet`,
with read access to the service repos (to read their contracts). Work proceeds
**one PR per service** (or small batch), each independently CI-verified.

## Onboarding checklist (per service)

1. Add `DKH.<Service>.Contracts` to `Directory.Packages.props` (`PackageVersion`) and to
   `DKH.McpGateway.Application/DKH.McpGateway.Application.csproj` (`PackageReference`).
2. Register the gRPC clients in `GrpcEndpointsRegistration.cs`
   (`grpc.AddEndpointFromConfiguration<TClient>()`), grouped with a `// <Service> (port)` comment.
3. Add the endpoint(s) to `appsettings*.json` under `Platform:Grpc:Endpoints`.
4. Create tools under `Tools/<Domain>/` — one tool per file, `static class` with
   `[McpServerToolType]`, `[McpServerTool(Name = "...")]`, typed `[Description]` params,
   `IApiKeyContext` permission checks (`Read`/`Write`), `McpJsonDefaults.Options` for JSON.
5. Apply the platform rules: multilingual (`lang`), **no PII**, code-based identification,
   `action` parameter for CRUD (`create/update/delete/get/list`).
6. Tests for every tool (xUnit + NSubstitute + FluentAssertions), mirroring existing tests.
7. Update `docs/en/mcp-tools.md` and the capabilities table in `CLAUDE.md`.

Scope decision: **full CRUD** where the service exposes management APIs; read-only where
the data is sensitive (Payment, Staff) or inherently query-only.

## Phased plan

### Phase 1 — Complete the commerce transaction loop (highest value)

| Service | Proposed MCP tools | Scope | Notes |
|---|---|---|---|
| **CartService** | `manage_cart` (CRUD/items), `manage_promo_code` (CRUD), `manage_cart_hold` (POS hold/resume), `manage_cart_claim` (claim codes) | CRUD | gRPC: CartCrud, CartManagement, CartHold, CartClaim, PromoCodeCrud (+event services are inbound-only — skip) |
| **PaymentService** | `get_payment`, `list_payments`, `get_payment_ledger` | **Read-only** | No card data / PII; statuses + ledger only |
| **DeliveryService** | `manage_shipment`, `get_delivery_status`, `list_claims`, `get_settlement` | CRUD (status), read for settlements | |
| **LogisticsService** | `calculate_rate`, `list_delivery_options`, `manage_tariff` | CRUD for tariffs, read for rates | Public-facing rate engine |

### Phase 2 — Supply & fulfillment operations

| Service | Proposed MCP tools | Scope | Notes |
|---|---|---|---|
| **WarehouseService** | `manage_warehouse`, `list_warehouses`, warehouse operations | CRUD | master data + operations workflows |
| **ProcurementService** | `manage_procurement_request`, `list_procurement`, inbound supply workflow | CRUD | |
| **CustomsService** | `lookup_hs_code`, `manage_declaration`, `list_certificates` | CRUD | HS codes, declarations, certificates |

### Phase 3 — Engagement, demand & partners

| Service | Proposed MCP tools | Scope | Notes |
|---|---|---|---|
| **NotificationService** | `get_notification_status`, `list_notifications` | **Read-only** | delivery logs; sending stays server-driven |
| **BroadcastService** | `manage_broadcast`, `list_broadcasts`, `get_broadcast_stats` | CRUD (schedule) | Telegram/WhatsApp/WeChat scheduled broadcasts |
| **ProductRequestService** | `manage_product_request`, `list_product_requests`, `get_request_stats` | CRUD | customer requests for missing products |
| **CounterpartyService** | `manage_counterparty`, `list_counterparties`, `get_counterparty` | CRUD | partner/counterparty registry; mind PII on contacts |

### Phase 4 — Platform & administration

| Service | Proposed MCP tools | Scope | Notes |
|---|---|---|---|
| **SubscriptionService** | `get_subscription`, `list_subscriptions`, `manage_subscription` | CRUD (state) | paid-product subscription state |
| **MediaService** | `list_media`, `get_media`, `manage_media` (metadata) | CRUD (metadata) | assets; do not stream binaries through MCP |
| **StaffService** | `get_staff_summary`, `list_roles` | **Read-only, no PII** | HR records — strict PII exclusion |
| **PrintService** | `list_printers`, `get_print_queue`, `manage_print_job` | CRUD | printer registry, queue, routing |
| **TelegramClientService** | `list_monitored_channels`, `get_channel_messages` | Read-only | MTProto channel monitoring |
| **AssistantService** | TBD — overlaps with the gateway itself | Discuss | AI assistant w/ Schema Discovery + RAG; decide relationship before integrating |

### Excluded — legacy / superseded

`DKH.CatalogService`, `DKH.CategoryService`, `DKH.Service.Scheduling` — appear superseded by
ProductCatalogService / ReferenceService. Confirm before discarding.

## Worked example — CartService (Phase 1, ready to execute)

From the contracts repo `DKH.CartService.Contracts` the gRPC services are:
`CartCrudService`, `CartManagementService`, `CartHoldService`, `CartClaimService`,
`PromoCodeCrudService` (plus inbound `*EventService` definitions consumed by workers —
not exposed via MCP). C# namespaces follow `DKH.CartService.Contracts.Cart.Api.<Module>.v1`.

Proposed tools:

- `manage_cart` — list/get carts (admin, optional storefront filter), via `CartCrudService`.
- `manage_cart_items` — add/update/remove items, via `CartManagementService`.
- `manage_cart_hold` — hold/resume carts for POS, via `CartHoldService`.
- `manage_cart_claim` — issue/redeem claim codes (QR + human-typeable), via `CartClaimService`.
- `manage_promo_code` — CRUD promo codes, via `PromoCodeCrudService`.

All with `lang` and the `action` parameter pattern; no PII in cart responses.

## Tracking

One GitHub issue per service, titled per `CONTRIBUTING.md` (`<type>(<scope>): <description>`)
and labelled `type:feature` (epic links them all and records phase ordering). Add issues to the
[GZDKH Project Board](https://github.com/orgs/GZDKH/projects/19).

Execution conventions (branch naming, commits, PR, quality gates, required reading) follow
`CONTRIBUTING.md` and `AGENTS.md` — see `mcp-onboarding-execution-plan.md`.
