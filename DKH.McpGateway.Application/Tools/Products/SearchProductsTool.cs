using DKH.SearchService.Contracts.Filters;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for searching products with filters, pagination, and optional semantic search.
/// Uses SearchService (Typesense) for hybrid keyword + vector search.
/// </summary>
[McpServerToolType]
public static class SearchProductsTool
{
    [McpServerTool(Name = "search_products"), Description("Search products in the catalog with optional filters for category, brand, and price range. Supports semantic search for natural language queries.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductSearchService.ProductSearchServiceClient client,
        [Description("Search query text")] string query,
        [Description("Catalog SEO name (e.g. 'main-catalog')")] string? catalogSeoName = null,
        [Description("Language code (e.g. 'en', 'ru')")] string? languageCode = null,
        [Description("Filter by brand SEO names (comma-separated)")] string? brandFilter = null,
        [Description("Minimum price filter")] double? priceMin = null,
        [Description("Maximum price filter")] double? priceMax = null,
        [Description("Enable semantic search (vector-based natural language matching)")] bool semanticSearch = false,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filterBy = TypesenseFilterBuilder.Create()
            .CatalogSeo(catalogSeoName)
            .LanguageCode(languageCode)
            .Brands(brandFilter?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .PriceRange(priceMin, priceMax)
            .Build();

        var request = new SearchProductsRequest
        {
            Query = query,
            FilterBy = filterBy,
            Page = page,
            PerPage = pageSize,
        };

        if (semanticSearch)
        {
            request.VectorQuery = $"embedding:([], k:{pageSize})";
        }

        var response = await client.SearchProductsAsync(request, cancellationToken: cancellationToken);

        var result = new
        {
            totalCount = response.Found,
            page,
            pageSize,
            searchTimeMs = response.SearchTimeMs,
            products = response.Hits.Select(static h => new
            {
                id = h.Document.Id,
                code = h.Document.Code,
                name = h.Document.Name,
                seoName = string.IsNullOrEmpty(h.Document.SeoName) ? h.Document.Code : h.Document.SeoName,
                price = h.Document.CallForPrice ? (double?)null : (double)h.Document.Price,
                currency = string.IsNullOrEmpty(h.Document.Currency) ? null : h.Document.Currency,
                brand = string.IsNullOrEmpty(h.Document.Brand) ? null : h.Document.Brand,
                inStock = h.Document.InStock,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
