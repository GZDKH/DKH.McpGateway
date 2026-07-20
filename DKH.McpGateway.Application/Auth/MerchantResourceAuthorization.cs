using DKH.McpGateway.Application.Tools.DataExchange;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Authorization contract for tenant-sensitive merchant MCP resources.
/// The SDK filter uses this policy to hide resources from unauthorized discovery,
/// while every resource method repeats the same guard before its first downstream call.
/// </summary>
internal static class MerchantResourceAuthorization
{
    internal const string PolicyName = "MerchantMcpResourceRead";

    internal static Metadata RequireReadAccess(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor)
    {
        var workspaceMetadata = ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            apiKeyContext,
            httpContextAccessor);
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        return workspaceMetadata;
    }
}

internal sealed class MerchantResourceRequirement : IAuthorizationRequirement;

internal sealed class MerchantResourceAuthorizationHandler(
    IApiKeyContext apiKeyContext,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<MerchantResourceRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MerchantResourceRequirement requirement)
    {
        try
        {
            _ = MerchantResourceAuthorization.RequireReadAccess(apiKeyContext, httpContextAccessor);
            context.Succeed(requirement);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or RpcException)
        {
            // Discovery must not expose merchant resources when transport, key scope,
            // permission, principal, or Workspace selection is invalid.
        }

        return Task.CompletedTask;
    }
}
