using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.SearchService.Contracts.Filters;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// MCP tool for finding products similar to a given product by sensory / descriptive closeness.
/// Resolves the source product card, then runs a semantic (vector) search over its
/// name and description, excluding the source product from the results.
/// </summary>
[McpServerToolType]
public static class SimilarProductsTool
{
    [McpServerTool(Name = "similar_products"), Description(
        "Recommend products similar to a given product by sensory / descriptive closeness. " +
        "Provide the source product slug; returns semantically nearest products (excluding the source). " +
        "Supports multilingual output (lang) and non-commercial mode (hides prices).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductManagementService.ProductManagementServiceClient catalogClient,
        ProductSearchService.ProductSearchServiceClient searchClient,
        [Description("Source product SEO name or slug")] string productSeoName,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code (e.g. 'en', 'ru')")] string lang = "ru",
        [Description("Maximum number of recommendations (max 50)")] int limit = 6,
        [Description("Non-commercial mode: hide prices and currency")] bool nonCommercial = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        limit = Math.Clamp(limit, 1, 50);

        var source = await catalogClient.GetProductDetailAsync(
            new GetProductDetailRequest
            {
                CatalogSeoName = catalogSeoName,
                ProductSeoName = productSeoName,
                LanguageCode = lang,
            },
            cancellationToken: cancellationToken);

        // Build a sensory profile query from the source product's name and description.
        var profile = $"{source.Name} {source.Description}".Trim();

        var filterBy = TypesenseFilterBuilder.Create()
            .CatalogSeo(catalogSeoName)
            .LanguageCode(lang)
            .Build();

        var request = new SearchProductsRequest
        {
            Query = string.IsNullOrWhiteSpace(profile) ? source.Name : profile,
            FilterBy = filterBy,
            Page = 1,
            // Fetch one extra so we can drop the source product and still return `limit` items.
            PerPage = limit + 1,
            VectorQuery = $"embedding:([], k:{limit + 1})",
        };

        var response = await searchClient.SearchProductsAsync(request, cancellationToken: cancellationToken);

        var similar = response.Hits
            .Where(h => !string.Equals(
                string.IsNullOrEmpty(h.Document.SeoName) ? h.Document.Code : h.Document.SeoName,
                productSeoName,
                StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(h => new
            {
                id = h.Document.Id,
                code = h.Document.Code,
                name = h.Document.Name,
                seoName = string.IsNullOrEmpty(h.Document.SeoName) ? h.Document.Code : h.Document.SeoName,
                price = nonCommercial || h.Document.CallForPrice ? (double?)null : (double)h.Document.Price,
                currency = nonCommercial || string.IsNullOrEmpty(h.Document.Currency) ? null : h.Document.Currency,
                brand = string.IsNullOrEmpty(h.Document.Brand) ? null : h.Document.Brand,
                inStock = h.Document.InStock,
            });

        var result = new
        {
            source = new
            {
                name = source.Name,
                seoName = string.IsNullOrEmpty(source.SeoName) ? productSeoName : source.SeoName,
            },
            lang,
            similar,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
