using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.DataExchange;

/// <summary>
/// Builds the explicit Workspace metadata required by merchant ProductCatalog data-exchange calls.
/// ProductCatalogService independently validates the selected Workspace against the propagated
/// authenticated caller, so this helper only accepts HTTP MCP-admin requests and never creates a
/// trusted/global or storefront-key bypass.
/// </summary>
internal static class ProductCatalogWorkspaceRequestContext
{
    public const string WorkspaceIdHeaderName = "X-Workspace-Id";

    internal const string GrpcWorkspaceIdHeaderName = "x-workspace-id";

    /// <summary>
    /// Requires one non-empty Workspace GUID on an authenticated HTTP MCP-admin request and returns
    /// fresh gRPC metadata containing only that normalized Workspace value.
    /// </summary>
    internal static Metadata CreateRequiredGrpcMetadata(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(apiKeyContext);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        if (!apiKeyContext.IsAuthenticated || apiKeyContext.Scope != ApiKeyScope.Mcp)
        {
            throw new UnauthorizedAccessException(
                "Product catalog data exchange requires an MCP-scoped API key.");
        }

        var httpContext = httpContextAccessor.HttpContext
            ?? throw new UnauthorizedAccessException(
                "Product catalog data exchange requires authenticated HTTP transport.");

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "Product catalog data exchange requires an authenticated HTTP principal.");
        }

        if (!httpContext.Request.Headers.TryGetValue(WorkspaceIdHeaderName, out var headerValues)
            || headerValues.Count != 1
            || string.IsNullOrWhiteSpace(headerValues[0])
            || !Guid.TryParse(headerValues[0], out var workspaceId)
            || workspaceId == Guid.Empty)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"The {WorkspaceIdHeaderName} header must contain exactly one non-empty GUID."));
        }

        return new Metadata
        {
            { GrpcWorkspaceIdHeaderName, workspaceId.ToString("D") },
        };
    }
}
