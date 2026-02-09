using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductSearchQuery.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for searching products with filters and pagination.
/// </summary>
[McpServerToolType]
public static class SearchProductsTool
{
    [McpServerTool(Name = "search_products"), Description("Search products in the catalog with optional filters for category, brand, and price range.")]
    public static async Task<string> ExecuteAsync(
        ProductSearchQueryService.ProductSearchQueryServiceClient client,
        [Description("Search query text")] string query,
        [Description("Catalog SEO name (e.g. 'main-catalog')")] string catalogSeoName = "main-catalog",
        [Description("Language code (e.g. 'en', 'ru')")] string languageCode = "ru",
        [Description("Filter by brand SEO names (comma-separated)")] string? brandFilter = null,
        [Description("Minimum price filter")] double? priceMin = null,
        [Description("Maximum price filter")] double? priceMax = null,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var request = new SearchProductsRequest
        {
            SearchTerm = query,
            CatalogSeoName = catalogSeoName,
            LanguageCode = languageCode,
            Page = page,
            PageSize = pageSize,
        };

        if (priceMin.HasValue)
        {
            request.MinPrice = priceMin.Value;
        }

        if (priceMax.HasValue)
        {
            request.MaxPrice = priceMax.Value;
        }

        if (!string.IsNullOrEmpty(brandFilter))
        {
            request.BrandSeoNames.AddRange(
                brandFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        var response = await client.SearchProductsAsync(request, cancellationToken: cancellationToken);

        var result = new
        {
            totalCount = response.TotalCount,
            page,
            pageSize,
            products = response.Items.Select(static p => new
            {
                id = p.Id,
                code = p.Code,
                name = p.Name,
                seoName = p.SeoName,
                price = p.CallForPrice ? (double?)null : p.Price,
                currency = p.CurrencyCode,
                brand = p.BrandName,
                inStock = p.InStock,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
