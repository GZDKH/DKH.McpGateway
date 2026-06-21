using DKH.SearchService.Contracts.Filters;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for listing catalog products with filters and pagination.
/// Unlike <see cref="SearchProductsTool"/> this returns a deterministic, browseable
/// catalog slice (no semantic ranking) — suited for content-API / catalog landing pages.
/// </summary>
[McpServerToolType]
public static class ListProductsTool
{
    [McpServerTool(Name = "list_products"), Description(
        "List catalog products with optional brand and price filters and pagination. " +
        "Deterministic catalog browsing (no semantic ranking). Use 'search_products' for natural-language search. " +
        "Supports multilingual output (lang) and non-commercial mode (hides prices).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductSearchService.ProductSearchServiceClient client,
        [Description("Catalog SEO name (e.g. 'main-catalog')")] string? catalogSeoName = null,
        [Description("Language code (e.g. 'en', 'ru')")] string? lang = null,
        [Description("Filter by brand SEO names (comma-separated)")] string? brandFilter = null,
        [Description("Minimum price filter")] double? priceMin = null,
        [Description("Maximum price filter")] double? priceMax = null,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 100)")] int pageSize = 20,
        [Description("Non-commercial mode: hide prices and currency")] bool nonCommercial = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var filterBy = TypesenseFilterBuilder.Create()
            .CatalogSeo(catalogSeoName)
            .LanguageCode(lang)
            .Brands(brandFilter?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .PriceRange(priceMin, priceMax)
            .Build();

        var request = new SearchProductsRequest
        {
            Query = "*",
            FilterBy = filterBy,
            Page = page,
            PerPage = pageSize,
        };

        var response = await client.SearchProductsAsync(request, cancellationToken: cancellationToken);

        var result = new
        {
            totalCount = response.Found,
            page,
            pageSize,
            lang,
            products = response.Hits.Select(h => new
            {
                id = h.Document.Id,
                code = h.Document.Code,
                name = h.Document.Name,
                seoName = string.IsNullOrEmpty(h.Document.SeoName) ? h.Document.Code : h.Document.SeoName,
                price = nonCommercial || h.Document.CallForPrice ? (double?)null : (double)h.Document.Price,
                currency = nonCommercial || string.IsNullOrEmpty(h.Document.Currency) ? null : h.Document.Currency,
                brand = string.IsNullOrEmpty(h.Document.Brand) ? null : h.Document.Brand,
                inStock = h.Document.InStock,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
