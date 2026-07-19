using DKH.SearchService.Contracts.Filters;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool that produces a grounded product recommendation from the catalog dataset.
/// It runs a semantic search over the user's stated need/preferences and returns ranked,
/// dataset-grounded candidates together with the search criteria, so the AI client can
/// explain the recommendation without hallucinating products.
/// </summary>
[McpServerToolType]
public static class AskExpertTool
{
    [McpServerTool(Name = "ask_expert"), Description(
        "Get a grounded product recommendation from the catalog dataset based on a natural-language need " +
        "(taste, occasion, preferences, constraints). Returns ranked candidates and the applied criteria so the " +
        "recommendation can be justified from real catalog data. Supports multilingual output (lang) and " +
        "non-commercial mode (hides prices). This tool does not invent products — it only ranks existing ones.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductSearchService.ProductSearchServiceClient client,
        [Description("Natural-language description of the need or preferences (e.g. 'a light floral green tea for mornings')")] string need,
        [Description("Catalog SEO name (e.g. 'main-catalog')")] string? catalogSeoName = null,
        [Description("Language code (e.g. 'en', 'ru')")] string lang = "ru",
        [Description("Filter by brand SEO names (comma-separated)")] string? brandFilter = null,
        [Description("Maximum price constraint")] double? priceMax = null,
        [Description("Number of candidates to recommend (max 20)")] int limit = 3,
        [Description("Non-commercial mode: hide prices and currency")] bool nonCommercial = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        limit = Math.Clamp(limit, 1, 20);

        var brands = brandFilter?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var filterBy = TypesenseFilterBuilder.Create()
            .CatalogSeo(catalogSeoName)
            .LanguageCode(lang)
            .Brands(brands)
            .PriceRange(null, priceMax)
            .Build();

        var request = new SearchProductsRequest
        {
            Query = need,
            FilterBy = filterBy,
            Page = 1,
            PerPage = limit,
            VectorQuery = $"embedding:([], k:{limit})",
        };

        var response = await client.SearchProductsAsync(request, cancellationToken: cancellationToken);

        var result = new
        {
            need,
            criteria = new
            {
                lang,
                catalogSeoName,
                brands = brands ?? Array.Empty<string>(),
                priceMax = nonCommercial ? null : priceMax,
            },
            recommendationCount = response.Hits.Count,
            recommendations = response.Hits.Take(limit).Select((h, index) => new
            {
                rank = index + 1,
                code = h.Document.Code,
                name = h.Document.Name,
                seoName = string.IsNullOrEmpty(h.Document.SeoName) ? h.Document.Code : h.Document.SeoName,
                brand = string.IsNullOrEmpty(h.Document.Brand) ? null : h.Document.Brand,
                price = nonCommercial || h.Document.CallForPrice ? (double?)null : (double)h.Document.Price,
                currency = nonCommercial || string.IsNullOrEmpty(h.Document.Currency) ? null : h.Document.Currency,
                inStock = h.Document.InStock,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
