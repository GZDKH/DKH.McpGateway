using DKH.McpGateway.Application.Tools.DataExchange;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;

using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for listing available product catalogs.
/// </summary>
[McpServerToolType]
public static class ListCatalogsTool
{
    [McpServerTool(Name = "list_catalogs"), Description("List all available product catalogs.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        CatalogManagementService.CatalogManagementServiceClient client,
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        // #85: bind the lookup to the selected, server-validated Workspace
        // BEFORE any downstream RPC — reuses the #83 resolver; ProductCatalog
        // independently verifies the propagated caller's membership.
        var workspaceMetadata = ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            apiKeyContext, httpContextAccessor);
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        var response = await client.GetCatalogsAsync(
            new GetStorefrontCatalogsRequest { LanguageCode = languageCode },
            headers: workspaceMetadata, cancellationToken: cancellationToken);

        var result = new
        {
            catalogs = response.Catalogs.Select(static c => new
            {
                name = c.Name,
                seoName = c.SeoName,
                productCount = c.ProductCount,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
