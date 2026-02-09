using DKH.ReviewService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Reviews;

[McpServerToolType]
public static class ReviewStatsTool
{
    [McpServerTool(Name = "review_stats"), Description("Get review statistics for a product: average score, total count, and 1-5 star rating distribution.")]
    public static async Task<string> ExecuteAsync(
        ReviewQueryService.ReviewQueryServiceClient client,
        [Description("Product ID (UUID)")] string productId,
        [Description("Storefront ID (UUID)")] string storefrontId,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetProductReviewAggregateAsync(
            new GetProductReviewAggregateRequest
            {
                StorefrontId = storefrontId,
                ProductId = productId,
            },
            cancellationToken: cancellationToken);

        var agg = response.Aggregate;
        var total = agg.TotalCount;

        var result = new
        {
            productId = agg.ProductId,
            storefrontId = agg.StorefrontId,
            averageScore = Math.Round(agg.AverageScore, 2),
            totalReviews = total,
            ratingDistribution = new
            {
                stars5 = new { count = agg.Count5, percentage = total > 0 ? Math.Round(100.0 * agg.Count5 / total, 1) : 0 },
                stars4 = new { count = agg.Count4, percentage = total > 0 ? Math.Round(100.0 * agg.Count4 / total, 1) : 0 },
                stars3 = new { count = agg.Count3, percentage = total > 0 ? Math.Round(100.0 * agg.Count3 / total, 1) : 0 },
                stars2 = new { count = agg.Count2, percentage = total > 0 ? Math.Round(100.0 * agg.Count2 / total, 1) : 0 },
                stars1 = new { count = agg.Count1, percentage = total > 0 ? Math.Round(100.0 * agg.Count1 / total, 1) : 0 },
            },
            sentiment = new
            {
                positive = agg.Count4 + agg.Count5,
                neutral = agg.Count3,
                negative = agg.Count1 + agg.Count2,
            },
            lastUpdated = agg.LastUpdatedAt?.ToDateTimeOffset().ToString("O"),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
