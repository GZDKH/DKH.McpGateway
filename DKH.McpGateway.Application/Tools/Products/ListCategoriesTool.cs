using DKH.McpGateway.Application.Tools.DataExchange;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;

using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for getting category tree for a catalog.
/// </summary>
[McpServerToolType]
public static class ListCategoriesTool
{
    [McpServerTool(Name = "list_categories"), Description("Get the category tree for a catalog with optional depth limit.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        CategoryManagementService.CategoryManagementServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        [Description("Maximum tree depth (0 = unlimited)")] int maxDepth = 0,
        CancellationToken cancellationToken = default)
    {
        // #85: bind the lookup to the selected, server-validated Workspace
        // BEFORE any downstream RPC — reuses the #83 resolver; ProductCatalog
        // independently verifies the propagated caller's membership.
        var workspaceMetadata = ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            apiKeyContext, httpContextAccessor);
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        var request = new GetCategoryTreeRequest
        {
            CatalogSeoName = catalogSeoName,
            LanguageCode = languageCode,
        };

        if (maxDepth > 0)
        {
            request.MaxDepth = maxDepth;
        }

        var response = await client.GetCategoryTreeAsync(request, headers: workspaceMetadata, cancellationToken: cancellationToken);

        var result = new
        {
            categories = response.RootCategories.Select(MapCategoryNode),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }

    private static object MapCategoryNode(CategoryNode node) => new
    {
        name = node.Name,
        seoName = node.SeoName,
        productCount = node.ProductCount,
        children = node.Children.Select(MapCategoryNode),
    };
}
