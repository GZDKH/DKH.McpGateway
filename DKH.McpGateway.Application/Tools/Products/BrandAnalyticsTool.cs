using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandQuery.v1;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class BrandAnalyticsTool
{
    [McpServerTool(Name = "brand_analytics"), Description("Get brand analytics: product counts and sorting by popularity or name.")]
    public static async Task<string> ExecuteAsync(
        BrandQueryService.BrandQueryServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        [Description("Sort by: productCount (default) or name")] string sortBy = "productCount",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetBrandsAsync(
            new GetBrandsRequest { CatalogSeoName = catalogSeoName, LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var totalProducts = response.Brands.Sum(b => (long)b.ProductCount);

        var brands = sortBy?.ToLowerInvariant() == "name"
            ? response.Brands.OrderBy(b => b.Name)
            : response.Brands.OrderByDescending(b => b.ProductCount);

        var result = new
        {
            totalBrands = response.Brands.Count,
            totalProducts,
            brands = brands.Select(b => new
            {
                name = b.Name,
                seoName = b.SeoName,
                productCount = b.ProductCount,
                percentage = totalProducts > 0 ? Math.Round((double)b.ProductCount / totalProducts * 100, 1) : 0,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
