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
    [McpServerTool(Name = "search_products"), Description("Search products within an explicit catalog scope, with optional brand/language filters and bounded semantic search. Financial filters are unsupported.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductSearchService.ProductSearchServiceClient client,
        [Description("Search query text")] string query,
        [Description("Catalog SEO name (e.g. 'main-catalog')")] string? catalogSeoName = null,
        [Description("Language code (e.g. 'en', 'ru')")] string? languageCode = null,
        [Description("Filter by brand SEO names (comma-separated)")] string? brandFilter = null,
        [Description("Legacy minimum price bound (unsupported; omit)")] double? priceMin = null,
        [Description("Legacy maximum price bound (unsupported; omit)")] double? priceMax = null,
        [Description("Enable semantic search (vector-based natural language matching)")] bool semanticSearch = false,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (priceMin.HasValue || priceMax.HasValue)
        {
            throw new ArgumentException("Financial price filters are unsupported by content discovery; omit the legacy price bounds.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(catalogSeoName);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filterBy = TypesenseFilterBuilder.Create()
            .CatalogSeo(catalogSeoName)
            .LanguageCode(languageCode)
            .Brands(brandFilter?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Build();

        var response = await ScopedProductSearch.ExecuteAsync(
            client, query, filterBy, page, pageSize, semanticSearch, cancellationToken);

        var result = new
        {
            totalCount = response.Found,
            page,
            pageSize,
            searchTimeMs = response.SearchTimeMs,
            products = response.Products.Select(static product => ProductSearchProjection.Create(product)),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
