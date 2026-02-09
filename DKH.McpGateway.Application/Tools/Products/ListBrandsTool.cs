using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.BrandQuery.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for listing all brands in a catalog.
/// </summary>
[McpServerToolType]
public static class ListBrandsTool
{
    [McpServerTool(Name = "list_brands"), Description("List all brands in the catalog with product counts.")]
    public static async Task<string> ExecuteAsync(
        BrandQueryService.BrandQueryServiceClient client,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetBrandsAsync(
            new GetBrandsRequest
            {
                CatalogSeoName = catalogSeoName,
                LanguageCode = languageCode,
            },
            cancellationToken: cancellationToken);

        var result = new
        {
            brands = response.Brands.Select(static b => new
            {
                name = b.Name,
                seoName = b.SeoName,
                description = b.Description,
                productCount = b.ProductCount,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
