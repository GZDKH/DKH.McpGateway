using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.ReviewService.Contracts.Review.Api.ReviewQuery.v1;

namespace DKH.McpGateway.Application.Tools.Reviews;

[McpServerToolType]
public static class ProductReviewRankingTool
{
    [McpServerTool(Name = "product_review_ranking"), Description("Rank products by review metrics: average rating or review count. Cross-references product catalog with review aggregates.")]
    public static async Task<string> ExecuteAsync(
        ProductManagementService.ProductManagementServiceClient searchClient,
        ReviewQueryService.ReviewQueryServiceClient reviewClient,
        [Description("Storefront ID (UUID)")] string storefrontId,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        [Description("Sort by: avgRating or reviewCount (default: avgRating)")] string sortBy = "avgRating",
        [Description("Number of top products to return (default 10, max 30)")] int limit = 10,
        [Description("Minimum number of reviews to include (default 3)")] int minReviews = 3,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 30);
        minReviews = Math.Max(minReviews, 1);

        var searchResponse = await searchClient.SearchProductsAsync(
            new SearchProductsRequest
            {
                CatalogSeoName = catalogSeoName,
                LanguageCode = languageCode,
                SearchTerm = string.Empty,
                Page = 1,
                PageSize = 100,
                InStock = true,
            },
            cancellationToken: cancellationToken);

        var productIds = searchResponse.Items.Select(p => p.Id).ToList();

        if (productIds.Count == 0)
        {
            return JsonSerializer.Serialize(new { products = Array.Empty<object>(), message = "No products found in catalog" }, McpJsonDefaults.Options);
        }

        var aggregatesResponse = await reviewClient.GetProductsReviewAggregatesAsync(
            new GetProductsReviewAggregatesRequest
            {
                StorefrontId = new GuidValue(storefrontId),
                ProductIds = { productIds.Select(id => new GuidValue(id)) },
            },
            cancellationToken: cancellationToken);

        var productMap = searchResponse.Items.ToDictionary(p => p.Id);
        var ranked = aggregatesResponse.Aggregates
            .Where(a => a.TotalCount >= minReviews)
            .OrderByDescending(a => sortBy == "reviewCount" ? a.TotalCount : a.AverageScore)
            .ThenByDescending(a => sortBy == "reviewCount" ? a.AverageScore : a.TotalCount)
            .Take(limit)
            .Select((a, idx) => new
            {
                rank = idx + 1,
                productId = a.ProductId,
                productName = productMap.TryGetValue(new GuidValue(a.ProductId), out var p) ? p.Name : null,
                averageScore = Math.Round(a.AverageScore, 2),
                totalReviews = a.TotalCount,
                positiveRate = a.TotalCount > 0
                    ? Math.Round(100.0 * (a.Count4 + a.Count5) / a.TotalCount, 1)
                    : 0,
            })
            .ToList();

        var result = new
        {
            sortedBy = sortBy,
            minReviewsThreshold = minReviews,
            totalProductsWithReviews = aggregatesResponse.Aggregates.Count(a => a.TotalCount >= minReviews),
            products = ranked,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
