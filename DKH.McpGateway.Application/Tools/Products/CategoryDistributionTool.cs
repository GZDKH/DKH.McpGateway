using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class CategoryDistributionTool
{
    [McpServerTool(Name = "category_distribution"), Description("Analyze product distribution across categories with counts and percentages.")]
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

        var totalProducts = response.RootCategories.Sum(c => (long)c.ProductCount);

        var result = new
        {
            totalProducts,
            categories = response.RootCategories.Select(c => MapNode(c, totalProducts)),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }

    private static object MapNode(CategoryNode node, long totalProducts) => new
    {
        name = node.Name,
        seoName = node.SeoName,
        productCount = node.ProductCount,
        percentage = totalProducts > 0 ? Math.Round((double)node.ProductCount / totalProducts * 100, 1) : 0,
        children = node.Children.Select(c => MapNode(c, totalProducts)),
    };
}
