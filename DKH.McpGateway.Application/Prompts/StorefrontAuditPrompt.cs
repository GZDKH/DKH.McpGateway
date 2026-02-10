namespace DKH.McpGateway.Application.Prompts;

[McpServerPromptType]
public static class StorefrontAuditPrompt
{
    [McpServerPrompt(Name = "storefront_audit")]
    [Description("Audit storefront configuration and identify potential issues with features, branding, domains, and channels.")]
    public static string Execute(
        [Description("Storefront code to audit, e.g. 'main'")] string storefrontCode)
    {
        return $"""
            You are a storefront configuration auditor for an e-commerce platform.

            Perform a comprehensive audit of the storefront '{storefrontCode}'.

            Steps:
            1. Use the 'get_storefront' tool with code='{storefrontCode}' to get basic info.
            2. Use the 'get_storefront_branding' tool to check branding configuration.
            3. Use the 'get_storefront_features' tool to review feature flags.
            4. Use the 'manage_storefront_domains' tool with action='list' to check domain setup.
            5. Use the 'manage_storefront_channels' tool with action='list' to review sales channels.
            6. Use the 'manage_storefront_catalogs' tool with action='list' to check catalog assignments.

            Provide an audit report covering:
            - Storefront status and basic health
            - Branding completeness (logo, colors, typography, favicon)
            - Feature flags review (which are enabled/disabled and recommendations)
            - Domain configuration (primary domain set? SSL verified?)
            - Sales channels (are expected channels configured?)
            - Catalog assignments (at least one catalog assigned? default set?)
            - Security considerations
            - Recommendations for improvement

            Rate each area as: OK, WARNING, or CRITICAL.
            Format the report in markdown with a summary table at the top.
            """;
    }
}
