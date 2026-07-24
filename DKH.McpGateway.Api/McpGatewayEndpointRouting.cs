using DKH.McpGateway.Application.Auth;

namespace DKH.McpGateway.Api;

internal static class McpGatewayEndpointRouting
{
    internal const string HttpEndpoint = "/mcp";

    internal static IEndpointConventionBuilder MapMcpGateway(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Ingress authentication only: a valid MCP-eligible API key (storefront or admin). The
        // privileged bearer requirement moved down to the admin tool surface so storefront-scoped
        // keys can reach the public storefront_* tools (issue #86).
        return endpoints
            .MapMcp(HttpEndpoint)
            .RequireAuthorization(McpAuthorizationPolicies.McpIngress);
    }
}
