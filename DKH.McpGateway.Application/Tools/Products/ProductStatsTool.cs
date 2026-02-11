using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class ProductStatsTool
{
    [McpServerTool(Name = "product_stats"), Description("Get product catalog statistics: total count, brand distribution, and top categories.")]
    public static async Task<string> ExecuteAsync(
        ProductManagementService.ProductManagementServiceClient searchClient,
        BrandManagementService.BrandManagementServiceClient brandClient,
        CategoryManagementService.CategoryManagementServiceClient categoryClient,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var productsTask = searchClient.SearchProductsAsync(
            new SearchProductsRequest
            {
                CatalogSeoName = catalogSeoName,
                LanguageCode = languageCode,
                SearchTerm = "",
                Page = 1,
                PageSize = 1,
            },
            cancellationToken: cancellationToken).ResponseAsync;

        var brandsTask = brandClient.GetBrandsAsync(
            new GetCatalogBrandsRequest { CatalogSeoName = catalogSeoName, LanguageCode = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        var categoriesTask = categoryClient.GetCategoryTreeAsync(
            new GetCategoryTreeRequest { CatalogSeoName = catalogSeoName, LanguageCode = languageCode, MaxDepth = 1 },
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(productsTask, brandsTask, categoriesTask);

        var brands = brandsTask.Result.Brands;
        var categories = categoriesTask.Result.RootCategories;

        var result = new
        {
            totalProducts = productsTask.Result.TotalCount,
            totalBrands = brands.Count,
            totalCategories = categories.Count,
            topBrands = brands
                .OrderByDescending(b => b.ProductCount)
                .Take(10)
                .Select(static b => new { name = b.Name, productCount = b.ProductCount }),
            topCategories = categories
                .OrderByDescending(c => c.ProductCount)
                .Take(10)
                .Select(static c => new { name = c.Name, seoName = c.SeoName, productCount = c.ProductCount }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
