namespace DKH.McpGateway.Tests.Auth;

public sealed class McpToolSurfaceTests
{
    private readonly McpToolSurface _surface = new();

    [Theory]
    [InlineData("storefront_list_catalogs")]
    [InlineData("storefront_get_category_tree")]
    [InlineData("storefront_get_product")]
    [InlineData("storefront_search_products")]
    [InlineData("storefront_recommend_products")]
    public void IsStorefrontPublic_ReturnsTrue_ForMarkedPublicTools(string toolName)
        => _surface.IsStorefrontPublic(toolName).Should().BeTrue();

    [Theory]
    // Admin tools whose name also starts with storefront_ MUST stay on the admin surface —
    // classification is by attribute, never by name prefix.
    [InlineData("storefront_overview")]
    [InlineData("storefront_audit")]
    [InlineData("search_products")]
    [InlineData("manage_product")]
    [InlineData("order_summary")]
    [InlineData("list_catalogs")]
    public void IsStorefrontPublic_ReturnsFalse_ForAdminTools(string toolName)
        => _surface.IsStorefrontPublic(toolName).Should().BeFalse();

    [Theory]
    [InlineData("")]
    [InlineData("unknown_tool")]
    [InlineData("STOREFRONT_SEARCH_PRODUCTS")] // case-sensitive, ordinal
    public void IsStorefrontPublic_ReturnsFalse_ForUnknownOrEmpty(string toolName)
        => _surface.IsStorefrontPublic(toolName).Should().BeFalse();

    [Fact]
    public void StorefrontPublicToolNames_AreExactlyTheFivePublicTools()
        => _surface.StorefrontPublicToolNames.Should().BeEquivalentTo(
            "storefront_list_catalogs",
            "storefront_get_category_tree",
            "storefront_get_product",
            "storefront_search_products",
            "storefront_recommend_products");
}
