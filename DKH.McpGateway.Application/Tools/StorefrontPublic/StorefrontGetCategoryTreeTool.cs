using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;

namespace DKH.McpGateway.Application.Tools.StorefrontPublic;

/// <summary>
/// MCP tool — public storefront namespace.
/// Returns the category tree for each of this storefront's catalogs. Requires a Scope.Storefront
/// API key; only the caller storefront's catalogs are walked (ADR-060 isolation).
/// </summary>
[McpServerToolType]
public static class StorefrontGetCategoryTreeTool
{
    [McpServerTool(Name = "storefront_get_category_tree"), Description(
        "Get the category tree for this storefront's catalogs. Requires a storefront-scoped API key.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IStorefrontMcpGate gate,
        StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient storefrontCatalogClient,
        CatalogManagementService.CatalogManagementServiceClient catalogClient,
        CategoryManagementService.CategoryManagementServiceClient categoryClient,
        [Description("Language code (e.g. 'en', 'ru')")] string languageCode = "ru",
        [Description("Maximum tree depth (0 = unlimited)")] int maxDepth = 0,
        CancellationToken cancellationToken = default)
    {
        var catalogSeoNames = await StorefrontScope.ResolveCatalogSeoNamesAsync(
            apiKeyContext, gate, storefrontCatalogClient, catalogClient, languageCode, cancellationToken);

        var catalogs = new List<object>();
        foreach (var catalogSeoName in catalogSeoNames)
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

            var response = await categoryClient.GetCategoryTreeAsync(request, cancellationToken: cancellationToken);

            catalogs.Add(new
            {
                catalogSeoName,
                categories = response.RootCategories.Select(MapCategoryNode),
            });
        }

        var result = new { catalogs };
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
