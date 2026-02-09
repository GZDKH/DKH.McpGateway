using DKH.ReviewService.Contracts.Api.V1;

namespace DKH.McpGateway.Application.Tools.Reviews;

[McpServerToolType]
public static class ReviewSummaryTool
{
    [McpServerTool(Name = "review_summary"), Description("Get product reviews grouped by sentiment (positive/neutral/negative) with sample review texts. No reviewer PII included.")]
    public static async Task<string> ExecuteAsync(
        ReviewQueryService.ReviewQueryServiceClient client,
        [Description("Product ID (UUID)")] string productId,
        [Description("Storefront ID (UUID)")] string storefrontId,
        [Description("Max reviews to fetch (default 50, max 100)")] int limit = 50,
        [Description("Sort: recent, rating, or helpful")] string sortBy = "recent",
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var response = await client.GetProductReviewsAsync(
            new GetProductReviewsRequest
            {
                StorefrontId = storefrontId,
                ProductId = productId,
                Page = 1,
                PageSize = limit,
                SortBy = sortBy,
            },
            cancellationToken: cancellationToken);

        var reviews = response.Items;

        var positive = reviews.Where(r => r.Review.Score >= 4).ToList();
        var neutral = reviews.Where(r => r.Review.Score == 3).ToList();
        var negative = reviews.Where(r => r.Review.Score <= 2).ToList();

        var result = new
        {
            productId,
            totalFetched = reviews.Count,
            sentiment = new
            {
                positive = new
                {
                    count = positive.Count,
                    samples = positive.Take(3).Select(r => new
                    {
                        score = r.Review.Score,
                        title = string.IsNullOrEmpty(r.Review.Title) ? null : r.Review.Title,
                        text = string.IsNullOrEmpty(r.Review.Body) ? null : Truncate(r.Review.Body, 200),
                    }),
                },
                neutral = new
                {
                    count = neutral.Count,
                    samples = neutral.Take(3).Select(r => new
                    {
                        score = r.Review.Score,
                        title = string.IsNullOrEmpty(r.Review.Title) ? null : r.Review.Title,
                        text = string.IsNullOrEmpty(r.Review.Body) ? null : Truncate(r.Review.Body, 200),
                    }),
                },
                negative = new
                {
                    count = negative.Count,
                    samples = negative.Take(3).Select(r => new
                    {
                        score = r.Review.Score,
                        title = string.IsNullOrEmpty(r.Review.Title) ? null : r.Review.Title,
                        text = string.IsNullOrEmpty(r.Review.Body) ? null : Truncate(r.Review.Body, 200),
                    }),
                },
            },
            hasStoreReplies = reviews.Any(r => r.Reply is not null),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : $"{text[..maxLength]}...";
    }
}
