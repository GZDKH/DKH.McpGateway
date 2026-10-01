# AGENTS.md

<!-- BEGIN REQUIRED-READING -->

## Required reading

Read the shared entrypoint before working:

1. [DKH.AgentRules](../../agents/DKH.AgentRules/AGENTS.md) — shared Codex safety and lifecycle rules.
2. [Codex trigger index](../../agents/DKH.AgentRules/rules/codex/triggers.md) — select the on-demand rule or skill for the workflow.

<!-- END REQUIRED-READING -->

## Repository scope

`DKH.McpGateway` is a stateless .NET MCP gateway. It exposes DKH functionality to
MCP clients over stdio and HTTP (Streamable HTTP at `/mcp`, with legacy trusted
client routes `/mcp/sse` and `/mcp/message`) and translates tool, resource, and
prompt calls into gRPC requests to downstream services.

- Target framework: .NET 10.
- HTTP port: 5013.
- Projects: `DKH.McpGateway.Api` owns transport/bootstrap; `DKH.McpGateway.Application`
  owns MCP tools, resources, prompts, auth, and gRPC registration.
- Do not add service implementations here; change the owning downstream repository
  and its contract package when the behavior belongs there.

## Local commands

Run commands from this repository or its leased worktree:

```bash
dotnet restore
dotnet build -c Release
dotnet test
dotnet format --verify-no-changes
dotnet run --project DKH.McpGateway.Api
```

Use the applicable shared .NET build gates before committing. Keep changes scoped
to the issue and inspect the exact diff before the GitLab MR.

## Runtime ownership

The request path is:

```text
MCP stdio/HTTP → tools/resources/prompts → gRPC clients → DKH services
```

`ConfigureServices.cs` registers the MCP server surface. Keep
`GrpcEndpointsRegistration.cs` one-for-one with the
`Platform:Grpc:Endpoints` configuration; use the shared config-sync workflow when
a client or endpoint is added or renamed. The gateway has no database.

Useful source locations:

- `DKH.McpGateway.Api/Program.cs` and `ConfigureServices.cs` — transport and DI.
- `DKH.McpGateway.Application/Tools/` — domain tool containers.
- `DKH.McpGateway.Application/Resources/` — read-only MCP resources.
- `DKH.McpGateway.Application/Prompts/` — analytics prompt templates.
- `DKH.McpGateway.Application/Auth/` — public/admin surface classification and guards.
- `Tools/Common/McpJsonDefaults.cs` — shared JSON serialization options.

Do not maintain hand-written counts of tools, containers, clients, or ports in this
file. Derive inventories from source and tests when documentation needs them.

## MCP surface and security

`StorefrontPublicToolAttribute` is the source of truth for the tenant-scoped
public surface. Public calls must pass `IStorefrontMcpGate` (`McpEnabled`) and are
constrained to the key's storefront. Unannotated or unknown tools are admin by
default; admin access requires an MCP-scoped API key and a bearer principal that
satisfies the MCP role policy. `ApiKeyAuthMiddleware` admits only MCP or
Storefront scopes.

Keep these boundaries in every tool/resource change:

- use `[McpServerToolType]` containers and `[McpServerTool]` methods;
- use `[StorefrontPublicTool]` only for deliberately public tenant operations;
- keep identifiers opaque when the downstream contract has no human-readable code;
- mask contacts, employee identities, customer IDs, credentials, and other PII;
- omit document bytes and secrets unless the explicit contract requires them;
- serialize responses through `McpJsonDefaults.Options`;
- accept translations as JSON arrays such as `[{"lang":"en","name":"..."}]`.

## Configuration and compatibility

- gRPC endpoints live under `Platform:Grpc:Endpoints` in appsettings.
- `Mcp:PublicEndpoint` must remain the canonical OAuth resource for every HTTP route.
- `Platform:Network:KnownProxies` contains only the real reverse proxy.
- `Platform:Auth:Keycloak:AdditionalAudiences` includes the canonical MCP resource audience.
- Keep `/mcp/sse` and `/mcp/message` compatibility behavior unless the issue explicitly
  removes it and the affected clients are verified.

## Verification checklist

Before the MR, run the relevant build/test/format gates and verify the MCP surface
with focused tests. For auth or public-surface changes, cover both an allowed
storefront request and a denied/default-closed request. For endpoint changes,
prove the registration/config one-for-one invariant and record the exact evidence
in the issue or MR. GitLab issues, MRs, and CI are authoritative; historical
GitHub links are read-only.
