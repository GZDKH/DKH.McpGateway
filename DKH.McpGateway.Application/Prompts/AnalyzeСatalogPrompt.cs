namespace DKH.McpGateway.Application.Prompts;

[McpServerPromptType]
public static class AnalyzeCatalogPrompt
{
    [McpServerPrompt(Name = "analyze_catalog")]
    [Description("Analyze product catalog health and provide recommendations. Checks category distribution, price ranges, stock levels, and data completeness.")]
    public static string Execute(
        [Description("Catalog SEO name to analyze, e.g. 'main-catalog'")] string catalogSeoName = "main-catalog",
        [Description("Language code for product names")] string languageCode = "ru")
    {
        return $"""
            You are a product catalog analyst for an e-commerce platform.

            Analyze the catalog '{catalogSeoName}' and provide actionable recommendations.

            Steps:
            1. Use the 'list_catalogs' tool to get an overview of available catalogs.
            2. Use the 'list_categories' tool with catalogSeoName='{catalogSeoName}' to get the category tree.
            3. Use the 'search_products' tool to check product distribution across categories.
            4. Use the 'get_product_stats' tool for overall catalog statistics.
            5. Use the 'get_category_distribution' tool to analyze category balance.

            Provide a report covering:
            - Total products and categories count
            - Category distribution (are products evenly spread or concentrated?)
            - Empty or underpopulated categories
            - Categories with excessive products that may need splitting
            - Price range analysis
            - Stock level concerns
            - Data quality issues (missing descriptions, images, etc.)
            - Specific recommendations for improvement

            Use language code '{languageCode}' for all tool calls that accept it.
            Format the report in markdown with clear sections and actionable items.
            """;
    }
}
