using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using Microsoft.AspNetCore.Authorization;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Ingress requirement for the <c>/mcp</c> endpoint: the request must carry a valid MCP-eligible
/// API key. <see cref="ApiKeyAuthMiddleware"/> validates the key and rejects missing/invalid/wrong-scope
/// keys before endpoint authorization runs, so this requirement only confirms an accepted
/// <see cref="ApiKeyScope.Mcp"/> or <see cref="ApiKeyScope.Storefront"/> context is present. It does
/// not require an authenticated bearer principal, so storefront-scoped keys reach the transport; the
/// admin/public split is enforced per tool surface (issue #86).
/// </summary>
internal sealed class McpIngressRequirement : IAuthorizationRequirement;

internal sealed class McpIngressAuthorizationHandler(IApiKeyContext apiKeyContext)
    : AuthorizationHandler<McpIngressRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        McpIngressRequirement requirement)
    {
        if (apiKeyContext.IsAuthenticated &&
            apiKeyContext.Scope is ApiKeyScope.Mcp or ApiKeyScope.Storefront)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
