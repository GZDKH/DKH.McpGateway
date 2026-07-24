using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.SearchService.Contracts.Filters;
using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using Grpc.Core;

namespace DKH.McpGateway.Application.Tools.StorefrontPublic;

/// <summary>
/// MCP tool — public storefront namespace.
/// Vector-based "more like this" recommendations seeded from one product, restricted to this
/// storefront's catalogs. Requires a Scope.Storefront API key; the seed product must itself live in
/// the storefront's catalogs and every recommendation is filtered to those catalogs, so a storefront
/// key can never seed from — nor surface — another tenant's products (ADR-060 isolation). For
/// multi-catalog storefronts each catalog is queried and the hits are merged by vector distance
/// (single-catalog storefronts keep the backend's exact ordering).
/// </summary>
[McpServerToolType]
[StorefrontPublicTool]
public static class StorefrontRecommendProductsTool
{
    [McpServerTool(Name = "storefront_recommend_products"), Description(
        "Recommend products similar to a given product, within this storefront's catalogs. Requires a storefront-scoped API key.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IStorefrontMcpGate gate,
        StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient storefrontCatalogClient,
        CatalogManagementService.CatalogManagementServiceClient catalogClient,
        ProductManagementService.ProductManagementServiceClient productClient,
        ProductSearchService.ProductSearchServiceClient searchClient,
        [Description("SEO name or slug of the product to base recommendations on")] string productSeoName,
        [Description("Language code (e.g. 'en', 'ru')")] string languageCode = "ru",
        [Description("Maximum number of recommendations (max 50)")] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var catalogSeoNames = await StorefrontScope.ResolveCatalogSeoNamesAsync(
            apiKeyContext, gate, storefrontCatalogClient, catalogClient, languageCode, cancellationToken);

        // Resolve the seed product within this storefront's catalogs only — a product outside them
        // stays unreachable, so recommendations can never be seeded from another tenant (isolation).
        var seedProductId = await ResolveSeedProductIdAsync(
            productClient, catalogSeoNames, productSeoName, languageCode, cancellationToken);

        maxResults = Math.Clamp(maxResults, 1, 50);

        // Query each of the storefront's catalogs and merge — the catalog filter on every call is the
        // isolation boundary, so recommendations stay inside the storefront's catalogs.
        var recommendations = new List<RecommendationModel>();
        foreach (var catalogSeoName in catalogSeoNames)
        {
            var filterBy = TypesenseFilterBuilder.Create()
                .CatalogSeo(catalogSeoName)
                .LanguageCode(languageCode)
                .Build();

            var response = await searchClient.GetRecommendationsAsync(
                new GetRecommendationsRequest
                {
                    ProductId = seedProductId,
                    MaxResults = maxResults,
                    FilterBy = filterBy,
                },
                cancellationToken: cancellationToken);

            recommendations.AddRange(response.Recommendations);
        }

        var result = new
        {
            seedSeoName = productSeoName,
            seedProductId,
            recommendations = recommendations
                .Where(r => r.Product.Id != seedProductId)
                .OrderBy(static r => r.VectorDistance)
                .DistinctBy(static r => r.Product.Id)
                .Take(maxResults)
                .Select(static r => new
                {
                    id = r.Product.Id,
                    code = r.Product.Code,
                    name = r.Product.Name,
                    seoName = string.IsNullOrEmpty(r.Product.SeoName) ? r.Product.Code : r.Product.SeoName,
                    price = r.Product.CallForPrice ? (double?)null : (double)r.Product.Price,
                    currency = string.IsNullOrEmpty(r.Product.Currency) ? null : r.Product.Currency,
                    brand = string.IsNullOrEmpty(r.Product.Brand) ? null : r.Product.Brand,
                    inStock = r.Product.InStock,
                    distance = r.VectorDistance,
                })
                .ToList(),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }

    /// <summary>
    /// Finds the product's id by walking only the storefront's catalogs. Throws <c>NotFound</c> when
    /// the product is not in any of them — the seed must belong to the caller's storefront.
    /// </summary>
    private static async Task<string> ResolveSeedProductIdAsync(
        ProductManagementService.ProductManagementServiceClient productClient,
        IReadOnlyList<string> catalogSeoNames,
        string productSeoName,
        string languageCode,
        CancellationToken cancellationToken)
    {
        foreach (var catalogSeoName in catalogSeoNames)
        {
            try
            {
                var product = await productClient.GetProductDetailAsync(
                    new GetProductDetailRequest
                    {
                        CatalogSeoName = catalogSeoName,
                        ProductSeoName = productSeoName,
                        LanguageCode = languageCode,
                    },
                    cancellationToken: cancellationToken);

                var id = product.Id?.Value;
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                // Not in this catalog — try the next one of the storefront's catalogs.
            }
        }

        throw new RpcException(new Status(
            StatusCode.NotFound,
            $"Product '{productSeoName}' was not found in this storefront's catalogs."));
    }
}
