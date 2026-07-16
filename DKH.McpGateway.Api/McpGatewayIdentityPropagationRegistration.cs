using DKH.Platform.Identity;
using DKH.Platform.Identity.Interceptors;

namespace DKH.McpGateway.Api;

/// <summary>
/// Registers the HTTP caller identity on trusted downstream gRPC clients.
/// </summary>
internal static class McpGatewayIdentityPropagationRegistration
{
    /// <summary>
    /// Registers the HTTP current-user abstraction and its outbound propagation interceptor.
    /// </summary>
    internal static IPlatformWebBuilder AddMcpHttpCurrentUserPropagation(this IPlatformWebBuilder platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        return platform
            .AddHttpCurrentUser()
            .AddCurrentUserPropagation();
    }

    /// <summary>
    /// Registers all MCP downstream clients and conditionally applies HTTP identity propagation.
    /// </summary>
    internal static void AddMcpGatewayEndpoints(
        this IPlatformGrpcClientBuilder grpc,
        bool propagateCurrentUser)
    {
        ArgumentNullException.ThrowIfNull(grpc);

        if (propagateCurrentUser)
        {
            grpc.AddGlobalInterceptor<PlatformUserIdentityPropagationInterceptor>();
        }

        grpc.AddMcpGatewayEndpoints();
    }
}
