namespace DKH.McpGateway.Api;

internal static class McpGatewayEndpointRouting
{
    internal const string HttpEndpoint = "/mcp";

    internal static IEndpointConventionBuilder MapMcpGateway(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints
            .MapMcp(HttpEndpoint)
            .RequireAuthorization(McpAuthorizationPolicies.McpAccess);
    }
}
