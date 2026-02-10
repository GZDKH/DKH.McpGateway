namespace DKH.McpGateway.Application.Prompts;

[McpServerPromptType]
public static class ReviewAnalysisPrompt
{
    [McpServerPrompt(Name = "review_analysis")]
    [Description("Analyze product reviews and customer sentiment. Identifies trends, common issues, and improvement areas.")]
    public static string Execute(
        [Description("Product SEO name to analyze reviews for (optional — omit for global analysis)")] string? productSeoName = null,
        [Description("Storefront code to filter by (optional)")] string? storefrontCode = null,
        [Description("Language code")] string languageCode = "ru")
    {
        var scope = !string.IsNullOrEmpty(productSeoName)
            ? $"product '{productSeoName}'"
            : "all products";

        var storefrontFilter = !string.IsNullOrEmpty(storefrontCode)
            ? $" on storefront '{storefrontCode}'"
            : "";

        return $"""
            You are a customer feedback analyst for an e-commerce platform.

            Analyze reviews for {scope}{storefrontFilter}.

            Steps:
            1. Use the 'search_reviews' tool to get recent reviews.
            2. If analyzing a specific product, use the 'get_product' tool with productSeoName='{productSeoName ?? ""}' to understand the product context.
            3. Use the 'get_review_stats' tool for aggregated review statistics.

            Provide an analysis covering:
            - Overall rating distribution (1-5 stars)
            - Average rating and trend direction
            - Common positive themes (what customers love)
            - Common negative themes (recurring complaints)
            - Sentiment analysis summary
            - Products with declining ratings (if global analysis)
            - Actionable recommendations for product/service improvement

            Important:
            - Do NOT include any personally identifiable information
            - Focus on patterns and trends, not individual reviews
            - Use language code '{languageCode}' for tool calls
            - Format the report in markdown with charts described in text
            """;
    }
}
