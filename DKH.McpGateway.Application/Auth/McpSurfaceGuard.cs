using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Per-request decision for the split MCP surface (issue #86): which tools the current credential
/// may discover and invoke. Ingress admits both storefront and admin callers; this guard keeps a
/// storefront-scoped key on the public <c>storefront_*</c> surface and reserves every other tool for
/// admin callers (default-closed).
/// </summary>
public sealed class McpSurfaceGuard(IApiKeyContext apiKeyContext, McpToolSurface surface)
{
    /// <summary>
    /// Admin authorization requires an <see cref="ApiKeyScope.Mcp"/> key <b>and</b> a bearer principal
    /// that satisfies the <see cref="McpAuthorizationPolicies.McpAccess"/> role policy. A storefront
    /// key never authorizes the admin surface, even paired with a privileged bearer — the key scope
    /// gates the surface. <paramref name="bearerSatisfiesMcpAccess"/> is the evaluated role-policy result.
    /// </summary>
    public bool IsAdminAuthorized(bool bearerSatisfiesMcpAccess)
        => apiKeyContext.Scope == ApiKeyScope.Mcp && bearerSatisfiesMcpAccess;

    /// <summary>
    /// <see langword="true"/> when the current credential may discover and invoke the named tool.
    /// Public storefront tools are reachable by storefront- or MCP-scoped keys; every admin/unknown
    /// tool requires admin authorization. Used identically for list filtering and call gating so a
    /// hidden tool can never be invoked by name.
    /// </summary>
    public bool CanAccessTool(string toolName, bool bearerSatisfiesMcpAccess)
    {
        if (surface.IsStorefrontPublic(toolName))
        {
            return apiKeyContext.Scope is ApiKeyScope.Storefront or ApiKeyScope.Mcp;
        }

        return IsAdminAuthorized(bearerSatisfiesMcpAccess);
    }
}
