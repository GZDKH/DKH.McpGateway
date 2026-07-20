# Operations

## Running locally

```bash
# HTTP transport (default, port 5013)
dotnet run --project DKH.McpGateway.Api

# stdio transport (for CLI-based MCP clients)
dotnet run --project DKH.McpGateway.Api -- --stdio

# stdio via environment variable
MCP_TRANSPORT=stdio dotnet run --project DKH.McpGateway.Api
```

## Docker

```bash
# Start with docker compose (from DKH.McpGateway root)
docker compose up -d

# Start as part of DKH.Infrastructure stack
docker compose -f docker-compose.services.yml --profile mcp up -d
```

## Transport modes

| Mode | Activation | Use case |
| ---- | ---------- | -------- |
| Streamable HTTP + legacy SSE | Default (no flags) | Authenticated network MCP clients |
| stdio | `--stdio` flag or `MCP_TRANSPORT=stdio` | CLI clients (Claude Code, Cursor) |

Both modes use the same `Platform.CreateWeb()` entry point and downstream client
set. HTTP mode additionally registers current-user propagation for its trusted
internal gRPC endpoints. Stdio has no HTTP identity and does not register that
interceptor; authenticated storefront provisioning and publishing are
HTTP-only.

ProductCatalog data exchange is HTTP-only. Configure the MCP HTTP client with
an MCP-scoped API key, an authenticated Keycloak session, and exactly one
`X-Workspace-Id` header. Missing, empty, invalid, or duplicate Workspace values
are rejected before a downstream call. Stdio and global execution without an
explicit Workspace are intentionally fail-closed.

The same HTTP requirements apply to generic `catalog://*` and
`storefront://*` resources. Unauthorized resources are removed from MCP
discovery and direct reads repeat the authorization check. Stdio exposes no
tenant-sensitive merchant resources, and the gateway does not cache their
results.

### Native OAuth clients

The canonical production MCP resource is exactly `https://thetea.app/mcp`
(without a trailing slash). The gateway publishes RFC 9728 protected-resource
metadata at
`https://thetea.app/.well-known/oauth-protected-resource/mcp` and advertises
the external Keycloak issuer and resource scope `mcp:tools`. HTTP MCP remains
dual-authenticated: OAuth provides the caller identity and realm role, while
`X-API-Key` independently provides MCP scope and `mcp:read` / `mcp:write`
permissions. `mcp:tools` activates the Keycloak audience mapper; it does not
replace either authorization gate.

Register the server in Codex without putting credentials in `config.toml`:

```bash
codex mcp add gzdkh-storefront \
  --url https://thetea.app/mcp \
  --oauth-client-id dkh-codex-local \
  --oauth-resource https://thetea.app/mcp
```

Then configure environment-backed headers under the generated server table:

```toml
[mcp_servers.gzdkh-storefront]
url = "https://thetea.app/mcp"
oauth_resource = "https://thetea.app/mcp"
env_http_headers = { "X-API-Key" = "DKH_MCP_API_KEY", "X-Workspace-Id" = "DKH_MCP_WORKSPACE_ID" }

[mcp_servers.gzdkh-storefront.oauth]
client_id = "dkh-codex-local"
```

Set `DKH_MCP_API_KEY` in the launching environment. Set
`DKH_MCP_WORKSPACE_ID` only for tools whose contract requires the trusted
Workspace header. Complete browser authorization with:

```bash
codex mcp login gzdkh-storefront --scopes mcp:tools
```

Never use a wildcard redirect URI, copy an access token into the config, or
replace the API-key permission boundary with OAuth scopes.

## Configuration

### gRPC endpoints

All downstream service addresses are configured in `Platform:Grpc:Endpoints` section:

```json
{
  "Platform": {
    "Grpc": {
      "Endpoints": {
        "ProductQueryServiceClient": {
          "Url": "http://localhost:5003",
          "TimeoutSeconds": 30
        }
      }
    }
  }
}
```

Environment-specific overrides:

- `appsettings.json` — local development (localhost)
- `appsettings.Docker.json` — Docker environment (service names)

### Environment variables

| Variable | Description | Default |
| -------- | ----------- | ------- |
| `ASPNETCORE_ENVIRONMENT` | Configuration environment | `Development` |
| `MCP_TRANSPORT` | Transport mode (`stdio` or empty) | empty (HTTP) |
| `Mcp__PublicEndpoint` | Exact canonical OAuth resource shared by all HTTP routes | `http://localhost:5013/mcp` |
| `Platform__Network__KnownProxies__0` | Trusted reverse-proxy address for forwarded scheme/host | none |
| `Platform__Auth__Keycloak__AdditionalAudiences__0` | Accepted native MCP OAuth audience | `https://thetea.app/mcp` |

## Logs

| Environment | Path |
| ----------- | ---- |
| Local | `logs/dkh-mcp-gateway-*.log` |
| Docker | `/app/logs/mcp-gateway-*.log` (volume: `dkh-mcp-gateway-api-logs`) |

Log configuration is in the `Logging` section of appsettings:

- Console: colored output with timestamps
- File: daily rolling, 50 MB limit, 14 days retention

## Health check

The HTTP transport exposes Streamable HTTP at `http://localhost:5013/mcp` and
the readiness probe at `http://localhost:5013/health/ready`.

Container and orchestrator health checks must use `/health/ready`, not an MCP
transport endpoint that requires both authentication gates.

## Downstream service dependencies

The gateway requires all downstream services to be running. In Docker, `depends_on` with `condition: service_healthy` ensures proper startup order.

| Service | Port | Required for |
| ------- | ---- | ------------ |
| product-catalog | 5003 | Product, category, tag tools |
| reference-service | 5004 | Reference, geography tools |
| order-service | 5007 | Order tools |
| storefront-service | 5009 | Storefront tools |
| review-service | 5011 | Review tools |
| telegram-bot | 5001 | Telegram tools |
