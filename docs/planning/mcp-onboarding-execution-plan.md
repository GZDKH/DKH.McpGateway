# MCP Gateway — Service Onboarding Execution Plan (Runbook)

A self-contained runbook for implementing the per-service MCP onboarding work tracked in
epic **#52** (issues **#53–#69**) and described in `services-onboarding-roadmap.md`.

This is written so a **fresh session in a properly-provisioned environment** can execute it
without re-deriving the codebase conventions. Follow it literally.

---

## 0. Required environment (must be true before starting)

The web sandbox used to draft the plan lacked these; the executing context MUST have them:

- **Repo scope**: read access to `GZDKH/DKH.McpGateway` **and** the target service repo
  (e.g. `GZDKH/DKH.CartService`) so its `.Contracts/proto` files are readable.
- **Private NuGet feed auth**: a working credential for `https://gitlab.thetea.app/...nuget`
  (see `nuget.config` in the repo) so `DKH.*.Contracts` packages restore.
- **.NET SDK 10** (see `global.json`: `10.0.102`, rollForward latestMajor, allowPrerelease).

Verify up front:

```bash
dotnet --version              # 10.0.x
dotnet restore                # must succeed (proves feed auth works)
dotnet build -c Release       # current main must be green before you start
dotnet test
```

If `dotnet restore` fails with 401, the feed credential is missing — stop and fix that first.

---

## 1. Repo conventions (non-negotiable — the build enforces them)

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `EnableNETAnalyzers`,
`EnforceCodeStyleInBuild`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=14`.
`.editorconfig` raises `category-Style` and `category-CodeQuality` to **warning** (= error),
and `IDE0055` (formatting) + `IDE0065` (using placement) to **error**.

Practical rules that keep the build green (verified against existing code):

- **Collection expressions**: use `[]` in target-typed positions (e.g. `x ?? []`), not
  `new List<T>()`/`Array.Empty<T>()` *where a target type exists*. `var x = new List<T>()`
  is fine (no target type). `Array.Empty<T>()` is also tolerated. Avoid object-creation in a
  target-typed conditional branch (IDE0028). When in doubt, declare `var x = new List<T>();`
  then `if (...) x = await ...;`.
- `var` vs explicit type is not enforced (IDE0007 suggestion, IDE0008 none) — match local style.
- `ToLowerInvariant()` is fine (used widely for `action` switches).
- `.ToList()` is fine. `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` is preferred.
- **No PII** ever (names, emails, phones, addresses, salaries, card data).
- JSON output via `McpJsonDefaults.Options`. Proto↔JSON via `McpProtoHelper.Parser`/`Formatter`.
- File-scoped namespaces; no file header; usings sorted; 4-space indent; final newline.
- Global usings already include: `System.ComponentModel`, `System.Text.Json`,
  `DKH.McpGateway.Application.Auth`, `DKH.McpGateway.Application.Tools.Common`,
  `DKH.Platform.Grpc.Common.Types`, `ModelContextProtocol.Server`.

---

## 2. Per-service procedure (run this loop for each issue #53–#69)

### Step 2.1 — Discover the contract (from the service repo)

Read the target service's `*.Contracts/proto/**` and record:

1. The **C# namespace(s)**: `option csharp_namespace = "DKH.<Svc>.Contracts.<Area>.Api.<Module>.v1";`
2. Each **gRPC service** name → generated client is `<ServiceName>.<ServiceName>Client`.
3. Each **rpc**: `rpc <Method>(<Request>) returns (<Response>);` — record method + message type names.
4. For list endpoints, note the response fields if you want a typed projection (otherwise format
   the whole response generically — see template B).

Also record the **published package version** of `DKH.<Svc>.Contracts` from the feed:

```bash
curl -s -u <user>:<token> \
  "https://gitlab.thetea.app/api/v4/groups/2/-/packages/nuget/metadata/dkh.<svc>.contracts/index.json" \
  | jq '.items[].items[].catalogEntry.version'
```

### Step 2.2 — Wire up the dependency

`Directory.Packages.props` — add under the contracts ItemGroup (use the real version):

```xml
<PackageVersion Include="DKH.CartService.Contracts" Version="X.Y.Z"/>
```

`DKH.McpGateway.Application/DKH.McpGateway.Application.csproj` — add under the contracts ItemGroup:

```xml
<PackageReference Include="DKH.CartService.Contracts"/>
```

### Step 2.3 — Register the gRPC clients

`DKH.McpGateway.Application/GrpcEndpointsRegistration.cs` — add a `using` for each module
namespace and, inside `AddMcpGatewayEndpoints`, a grouped block:

```csharp
// CartService (<port>)
grpc.AddEndpointFromConfiguration<CartCrudService.CartCrudServiceClient>();
grpc.AddEndpointFromConfiguration<CartManagementService.CartManagementServiceClient>();
grpc.AddEndpointFromConfiguration<CartHoldService.CartHoldServiceClient>();
grpc.AddEndpointFromConfiguration<CartClaimService.CartClaimServiceClient>();
grpc.AddEndpointFromConfiguration<PromoCodeCrudService.PromoCodeCrudServiceClient>();
```

`AddEndpointFromConfiguration<TClient>()` resolves config by the client type name. Add the
matching endpoint to `DKH.McpGateway.Api/appsettings.json` (and `appsettings.Development.json`)
under `Platform:Grpc:Endpoints` — mirror an existing entry; confirm the service's gRPC port.

### Step 2.4 — Write the tools (one tool per file, `Tools/<Domain>/`)

**Template A — generic CRUD tool** (preferred; needs only method + message type names, not
message fields). Mirrors the existing `ManageTagsTool` / `ManageSpecAttributeTool`.

```csharp
using DKH.CartService.Contracts.Cart.Api.PromoCodeCrud.v1;

namespace DKH.McpGateway.Application.Tools.Cart;

[McpServerToolType]
public static class ManagePromoCodeTool
{
    [McpServerTool(Name = "manage_promo_code"), Description(
        "Manage promo codes: create, update, delete, get, or list. " +
        "For create/update/delete/get pass the matching request as JSON. " +
        "For list pass optional filter JSON.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PromoCodeCrudService.PromoCodeCrudServiceClient client,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Request payload as JSON (proto JSON for the chosen action)")] string? json = null,
        CancellationToken cancellationToken = default)
    {
        switch (action.ToLowerInvariant())
        {
            case "create":
                apiKeyContext.EnsurePermission(McpPermissions.Write);
                return Format(await client.CreatePromoCodeAsync(
                    Parse<CreatePromoCodeRequest>(json), cancellationToken: cancellationToken));
            case "update":
                apiKeyContext.EnsurePermission(McpPermissions.Write);
                return Format(await client.UpdatePromoCodeAsync(
                    Parse<UpdatePromoCodeRequest>(json), cancellationToken: cancellationToken));
            case "delete":
                apiKeyContext.EnsurePermission(McpPermissions.Write);
                return Format(await client.DeletePromoCodeAsync(
                    Parse<DeletePromoCodeRequest>(json), cancellationToken: cancellationToken));
            case "get":
                apiKeyContext.EnsurePermission(McpPermissions.Read);
                return Format(await client.GetPromoCodeAsync(
                    Parse<GetPromoCodeRequest>(json), cancellationToken: cancellationToken));
            case "list":
                apiKeyContext.EnsurePermission(McpPermissions.Read);
                return Format(await client.ListPromoCodesAsync(
                    Parse<ListPromoCodesRequest>(json ?? "{}"), cancellationToken: cancellationToken));
            default:
                return McpProtoHelper.FormatError(
                    $"Unknown action '{action}'. Use: create, update, delete, get, or list");
        }
    }

    private static T Parse<T>(string? json) where T : Google.Protobuf.IMessage<T>, new()
        => McpProtoHelper.Parser.Parse<T>(string.IsNullOrWhiteSpace(json) ? "{}" : json);

    private static string Format(Google.Protobuf.IMessage message)
        => McpProtoHelper.Formatter.Format(message);
}
```

> Why generic: `McpProtoHelper.Parser`/`Formatter` handle every field automatically, so you do
> not need the message internals — only the exact rpc method names and request/response type
> names from the proto. This is the lowest-risk way to onboard a service quickly.
> Keep the exact method names from the proto (e.g. `CreateAsync` vs `CreatePromoCodeAsync`).

**Template B — read-only / typed projection** (for sensitive services where you must shape the
output, e.g. Payment/Staff — drop PII fields explicitly). Mirror `GetProductTool`/`SearchProductsTool`:
fetch, then build an anonymous object with only the allowed fields and serialize with
`McpJsonDefaults.Options`. Add `lang`/`languageCode` and (for catalog-ish data) a `nonCommercial`
flag where prices apply.

Add `[Description("Language code (e.g. 'en', 'ru')")] string lang = "ru"` where the request
supports localization, and pass it into the request.

### Step 2.5 — Tests (one test class per tool)

Mirror `tests/.../Tools/Tags`/`Products`. Use xUnit + NSubstitute + the helpers in
`tests/.../Infrastructure` (`ApiKeyContextMocks.FullAccess()/ReadOnly()`, `GrpcTestHelpers`).
gRPC client methods are mocked with the 4-arg overload:

```csharp
private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
private readonly PromoCodeCrudService.PromoCodeCrudServiceClient _client =
    Substitute.For<PromoCodeCrudService.PromoCodeCrudServiceClient>();

[Fact]
public async Task List_CallsClientAndReturnsJsonAsync()
{
    _client.ListPromoCodesAsync(
            Arg.Any<ListPromoCodesRequest>(),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
        .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListPromoCodesResponse()));

    var result = await ManagePromoCodeTool.ExecuteAsync(_auth, _client, "list");

    JsonDocument.Parse(result); // valid JSON
    _ = _client.Received(1).ListPromoCodesAsync(
        Arg.Any<ListPromoCodesRequest>(),
        Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
}
```

Cover per tool: happy path per action, unknown-action error, read-only key denied on write
(`ApiKeyContextMocks.ReadOnly()` throws on `EnsurePermission(Write)`), and an RpcException
propagation (`GrpcTestHelpers.CreateFaultedAsyncUnaryCall<T>(StatusCode.Unavailable)`).

### Step 2.6 — Docs

Update `docs/en/mcp-tools.md` (add a section/rows for the service) and the capabilities table
in `CLAUDE.md`. Keep counts accurate.

### Step 2.7 — Verify & open PR

```bash
dotnet build -c Release          # zero warnings (they are errors)
dotnet test
dotnet format --verify-no-changes
```

Branch `claude/mcp-onboard-<service>` off `main`; **one draft PR per service**; link the issue
(`Closes #53`). Do not push to `main`.

---

## 3. Ordering & parallelization

All services touch the same shared files (`Directory.Packages.props`,
`GrpcEndpointsRegistration.cs`, `appsettings*.json`, `docs/en/mcp-tools.md`, `CLAUDE.md`).
Therefore:

- **Do not run services fully in parallel on separate branches** — they will conflict on the
  shared files and create merge churn.
- Recommended: **sequential, one PR per service**, in phase order (Cart → Payment → Delivery →
  Logistics → …). Merge or rebase between them.
- If parallelism is required, split by *non-overlapping* work: one agent drafts the per-service
  **tool + test files** (new files, no conflict) while a single integrator serially applies the
  shared-file edits (package refs, registration, appsettings, docs). Keep the shared-file edits
  on one branch to avoid conflicts.

Per-service scope (full CRUD vs read-only / PII notes) is in each issue #53–#69 and the roadmap.

---

## 4. Per-service quick index

| Issue | Service | Scope | gRPC services (confirm from proto) |
|---|---|---|---|
| #53 | CartService | CRUD | CartCrud, CartManagement, CartHold, CartClaim, PromoCodeCrud (skip `*Event`) |
| #54 | PaymentService | read-only | payment query + ledger (confirm) — **no card data/PII** |
| #55 | DeliveryService | CRUD/read | shipments, claims, settlements (confirm) |
| #56 | LogisticsService | CRUD/read | rates, tariffs, delivery options (confirm) |
| #57 | WarehouseService | CRUD | master data + operations (confirm) |
| #58 | ProcurementService | CRUD | inbound supply workflow (confirm) |
| #59 | CustomsService | CRUD | HS codes, declarations, certificates (confirm) |
| #60 | NotificationService | read-only | delivery status/log (confirm) — **no recipient PII** |
| #61 | BroadcastService | CRUD | broadcast schedule + stats (confirm) |
| #62 | ProductRequestService | CRUD | product requests + stats (confirm) |
| #63 | CounterpartyService | CRUD | counterparty registry (confirm) — **mind contact PII** |
| #64 | SubscriptionService | CRUD | subscription state (confirm) |
| #65 | MediaService | CRUD (metadata) | asset metadata (confirm) — **no binary streaming** |
| #66 | StaffService | read-only | aggregate only — **strict NO PII** |
| #67 | PrintService | CRUD | printers, queue, jobs (gRPC only; not SignalR) |
| #68 | TelegramClientService | read-only | channels, messages (confirm) — **privacy** |
| #69 | AssistantService | decision first | overlaps the gateway — resolve before building |

Legacy to skip (confirm with team): CatalogService, CategoryService, Service.Scheduling.

---

## 5. Definition of done (per service)

- [ ] `DKH.<Svc>.Contracts` referenced at the real published version; `dotnet restore` clean.
- [ ] Clients registered; `appsettings` endpoint added.
- [ ] Tools per the issue scope; CRUD via Template A, sensitive/typed via Template B; `lang`; no PII.
- [ ] Tests per tool (happy/unknown-action/permission/RpcException).
- [ ] `docs/en/mcp-tools.md` + `CLAUDE.md` updated.
- [ ] `dotnet build -c Release` (no warnings), `dotnet test`, `dotnet format --verify-no-changes` all green.
- [ ] Draft PR opened, linked to the issue.
