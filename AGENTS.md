# DKH.McpGateway

<!-- BEGIN REQUIRED-READING -->

Before GZDKH work, read `agents/DKH.AgentRules/AGENTS.md` and
`agents/DKH.AgentRules/rules/codex/triggers.md`. Use the GZDKH workspace
supplied for the task if it contains both files. Otherwise search the ancestors
of `git rev-parse --path-format=absolute --git-common-dir` for both files.
If neither location contains both files, ask for the GZDKH workspace path.
Open further rules only when triggered.

<!-- END REQUIRED-READING -->

Stateless MCP transport/tool adapter; domain implementation belongs in downstream
services. `StorefrontPublicToolAttribute` defines the deliberately public surface.
Public calls must pass `IStorefrontMcpGate`/McpEnabled and remain within the key's
storefront. Unknown/unannotated tools default to admin; admin access requires an
MCP-scoped key and the bearer role policy. The API-key middleware admits only MCP
or Storefront scopes.

Preserve opaque IDs, mask PII and omit secrets/document bytes unless the explicit
contract requires them. Serialize with `McpJsonDefaults.Options`; translations
remain JSON arrays. Keep gRPC registrations and configuration one-for-one.

`Mcp:PublicEndpoint` is the canonical OAuth resource on every HTTP route;
KnownProxies contains only actual proxies. Keep legacy trusted-client MCP routes
until their removal is explicitly scoped and affected clients verified. Auth
changes need both allowed and denied/default-closed cases.

For setup or current verification commands, read `README.md` and the actual
project manifests/CI.
