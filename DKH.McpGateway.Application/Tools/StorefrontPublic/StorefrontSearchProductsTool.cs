using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.SearchService.Contracts.Filters;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;

namespace DKH.McpGateway.Application.Tools.StorefrontPublic;

/// <summary>
/// MCP tool — public storefront namespace.
/// Full-text product search restricted to this storefront's catalogs. Requires a Scope.Storefront
/// API key; the catalog filter is always populated from the caller's storefront, so a search can
/// never span other tenants (ADR-060 isolation). For multi-catalog storefronts each catalog is
/// searched and the hits are concatenated (single-catalog storefronts keep exact relevance order).
/// </summary>
[McpServerToolType]
public static class StorefrontSearchProductsTool
{
    [McpServerTool(Name = "storefront_search_products"), Description(
        "Search products within this storefront's catalogs. Requires a storefront-scoped API key.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IStorefrontMcpGate gate,
        StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient storefrontCatalogClient,
        CatalogManagementService.CatalogManagementServiceClient catalogClient,
        ProductSearchService.ProductSearchServiceClient searchClient,
        [Description("Search query text")] string query,
        [Description("Language code (e.g. 'en', 'ru')")] string? languageCode = null,
        [Description("Page size (max 100)")] int pageSize = 20,
        [Description("Enable semantic (vector) search for natural-language queries like 'smoky oolong with caramel'")] bool semanticSearch = false,
        CancellationToken cancellationToken = default)
    {
        var catalogSeoNames = await StorefrontScope.ResolveCatalogSeoNamesAsync(
            apiKeyContext, gate, storefrontCatalogClient, catalogClient, languageCode ?? "ru", cancellationToken);

        pageSize = Math.Clamp(pageSize, 1, 100);

        var products = new List<object>();
        long totalFound = 0;
        foreach (var catalogSeoName in catalogSeoNames)
        {
            var filterBy = TypesenseFilterBuilder.Create()
                .CatalogSeo(catalogSeoName)
                .LanguageCode(languageCode)
                .Build();

            var request = new SearchProductsRequest
            {
                Query = query,
                FilterBy = filterBy,
                Page = 1,
                PerPage = pageSize,
            };

            if (semanticSearch)
            {
                request.VectorQuery = $"embedding:([], k:{pageSize})";
            }

            var response = await searchClient.SearchProductsAsync(request, cancellationToken: cancellationToken);

            totalFound += response.Found;
            products.AddRange(response.Hits.Select(static h => new
            {
                id = h.Document.Id,
                code = h.Document.Code,
                name = h.Document.Name,
                seoName = string.IsNullOrEmpty(h.Document.SeoName) ? h.Document.Code : h.Document.SeoName,
                price = h.Document.CallForPrice ? (double?)null : (double)h.Document.Price,
                currency = string.IsNullOrEmpty(h.Document.Currency) ? null : h.Document.Currency,
                brand = string.IsNullOrEmpty(h.Document.Brand) ? null : h.Document.Brand,
                inStock = h.Document.InStock,
            }));
        }

        var result = new
        {
            totalCount = totalFound,
            pageSize,
            products = products.Take(pageSize),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
