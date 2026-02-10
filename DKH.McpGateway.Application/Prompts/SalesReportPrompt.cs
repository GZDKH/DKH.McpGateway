namespace DKH.McpGateway.Application.Prompts;

[McpServerPromptType]
public static class SalesReportPrompt
{
    [McpServerPrompt(Name = "sales_report")]
    [Description("Generate a sales summary report for a given period. Includes order trends, top products, and revenue breakdown.")]
    public static string Execute(
        [Description("Period start in ISO 8601 format, e.g. '2025-01-01'")] string periodStart,
        [Description("Period end in ISO 8601 format, e.g. '2025-01-31'")] string periodEnd,
        [Description("Storefront code to filter by (optional)")] string? storefrontCode = null)
    {
        var storefrontFilter = string.IsNullOrEmpty(storefrontCode)
            ? "across all storefronts"
            : $"for storefront '{storefrontCode}'";

        return $"""
            You are a sales analytics assistant for an e-commerce platform.

            Generate a comprehensive sales report {storefrontFilter} for the period {periodStart} to {periodEnd}.

            Steps:
            1. Use the 'search_orders' tool to get orders in the date range.
            2. Use the 'get_product_stats' tool for product performance data.
            3. Use the 'get_brand_analytics' tool to understand brand performance.
            4. Use the 'list_storefronts' tool to identify active storefronts.

            Provide a report covering:
            - Total orders and revenue for the period
            - Order status breakdown (completed, pending, cancelled)
            - Average order value
            - Top-selling products by quantity and revenue
            - Brand performance comparison
            - Day-over-day or week-over-week trends if the period is long enough
            - Comparison to the previous equivalent period if possible

            Important:
            - Do NOT include any personally identifiable information (PII)
            - Aggregate all data — no individual customer details
            - Format the report in markdown with tables where appropriate
            """;
    }
}
