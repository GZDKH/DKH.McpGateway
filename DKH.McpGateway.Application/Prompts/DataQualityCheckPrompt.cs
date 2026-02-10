namespace DKH.McpGateway.Application.Prompts;

[McpServerPromptType]
public static class DataQualityCheckPrompt
{
    [McpServerPrompt(Name = "data_quality_check")]
    [Description("Check data quality across the platform. Finds missing translations, incomplete products, orphan categories, and reference data gaps.")]
    public static string Execute(
        [Description("Scope: 'products', 'references', or 'all'")] string scope = "all",
        [Description("Language code to check translations for")] string languageCode = "ru")
    {
        return $"""
            You are a data quality engineer for an e-commerce platform.

            Perform a data quality check with scope='{scope}'.

            {(scope is "products" or "all" ? """
            Product data checks:
            1. Use 'list_catalogs' to enumerate all catalogs.
            2. Use 'list_categories' for each catalog to find empty categories.
            3. Use 'search_products' to sample products and check completeness.
            4. Use 'get_product_stats' for overall statistics.
            5. Use 'list_brands' to check for brands without products.

            Check for:
            - Products missing descriptions or short descriptions
            - Products without images
            - Products with price = 0 or negative prices
            - Products without brand or category assignment
            - Empty categories (no products)
            - Orphan categories (deep nesting with no products)
            - Brands with zero products assigned
            - Missing or inconsistent SKU patterns
            """ : "")}

            {(scope is "references" or "all" ? $"""
            Reference data checks:
            1. Use the 'reference://countries' resource to list all countries.
            2. Use the 'reference://currencies' resource to list all currencies.
            3. Use the 'reference://languages' resource to list all languages.
            4. Use 'list_measurements' to check measurement units.
            5. Use 'list_delivery_times' to check delivery options.

            Check for:
            - Missing translations for language code '{languageCode}'
            - Countries without state/province data (where expected)
            - Currencies without proper symbols
            - Delivery times with unreasonable day ranges
            - Measurement units without conversion ratios
            """ : "")}

            Provide a report covering:
            - Data completeness score (percentage)
            - Critical issues (blocking for customers)
            - Warnings (degraded experience)
            - Informational findings
            - Prioritized list of fixes

            Format the report in markdown with severity indicators.
            """;
    }
}
