namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Authorization policy names for the MCP gateway HTTP surface.
/// </summary>
public static class McpAuthorizationPolicies
{
    /// <summary>
    /// Ingress policy for the <c>/mcp</c> endpoint. Succeeds when the request carries a valid
    /// MCP-eligible API key (<see cref="ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1.ApiKeyScope.Mcp"/>
    /// or <see cref="ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1.ApiKeyScope.Storefront"/>),
    /// already validated by <see cref="ApiKeyAuthMiddleware"/>. It authenticates the connection
    /// without requiring a privileged bearer, so storefront-scoped keys reach the transport; the
    /// admin/public split is then enforced per tool surface, not at ingress (issue #86).
    /// </summary>
    public const string McpIngress = "McpIngress";

    /// <summary>
    /// Admin-surface policy: the caller's bearer principal must hold a privileged MCP realm role.
    /// Combined with an <c>Mcp</c>-scoped key (and per-call Workspace) this authorizes the admin
    /// tool surface. Storefront-scoped keys never satisfy the admin surface even with this role.
    /// </summary>
    public const string McpAccess = "McpAccess";
}
