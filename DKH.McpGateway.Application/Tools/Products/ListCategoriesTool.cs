using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for getting category tree for a catalog.
/// </summary>
[McpServerToolType]
public static class ListCategoriesTool
{
    [McpServerTool(Name = "list_categories"), Description("Get the category tree for a catalog with optional depth limit.")]
    public static async Task<string> ExecuteAsync(
        CategoryManagementService.CategoryManagementServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        [Description("Maximum tree depth (0 = unlimited)")] int maxDepth = 0,
        CancellationToken cancellationToken = default)
    {
        var request = new GetCategoryTreeRequest
        {
            CatalogSeoName = catalogSeoName,
            LanguageCode = languageCode,
        };

        if (maxDepth > 0)
        {
            request.MaxDepth = maxDepth;
        }

        var response = await client.GetCategoryTreeAsync(request, cancellationToken: cancellationToken);

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
