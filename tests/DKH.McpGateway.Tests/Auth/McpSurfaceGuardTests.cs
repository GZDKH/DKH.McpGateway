using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;

namespace DKH.McpGateway.Tests.Auth;

public sealed class McpSurfaceGuardTests
{
    private const string PublicTool = "storefront_search_products";
    private const string AdminTool = "search_products";
    private const string AdminStorefrontPrefixedTool = "storefront_overview";

    private readonly McpToolSurface _surface = new();

    private McpSurfaceGuard Guard(ApiKeyScope scope)
    {
        var apiKey = Substitute.For<IApiKeyContext>();
        apiKey.Scope.Returns(scope);
        return new McpSurfaceGuard(apiKey, _surface);
    }

    // --- Public storefront surface ---

    [Fact]
    public void StorefrontKey_CanAccess_PublicTool()
        => Guard(ApiKeyScope.Storefront).CanAccessTool(PublicTool, bearerSatisfiesMcpAccess: false)
            .Should().BeTrue();

    [Fact]
    public void McpKey_CanAccess_PublicTool()
        => Guard(ApiKeyScope.Mcp).CanAccessTool(PublicTool, bearerSatisfiesMcpAccess: false)
            .Should().BeTrue();

    // --- Admin surface: storefront keys are always denied ---

    [Theory]
    [InlineData(AdminTool)]
    [InlineData(AdminStorefrontPrefixedTool)]
    public void StorefrontKey_CannotAccess_AdminTool_EvenWithBearer(string toolName)
    {
        Guard(ApiKeyScope.Storefront).CanAccessTool(toolName, bearerSatisfiesMcpAccess: true)
            .Should().BeFalse();
        Guard(ApiKeyScope.Storefront).IsAdminAuthorized(bearerSatisfiesMcpAccess: true)
            .Should().BeFalse();
    }

    // --- Admin surface: MCP key requires the privileged bearer ---

    [Fact]
    public void McpKey_WithBearer_CanAccess_AdminTool()
        => Guard(ApiKeyScope.Mcp).CanAccessTool(AdminTool, bearerSatisfiesMcpAccess: true)
            .Should().BeTrue();

    [Fact]
    public void McpKey_WithoutBearer_CannotAccess_AdminTool()
        => Guard(ApiKeyScope.Mcp).CanAccessTool(AdminTool, bearerSatisfiesMcpAccess: false)
            .Should().BeFalse();

    // --- Default-closed: unknown tools are treated as admin ---

    [Fact]
    public void UnknownTool_IsTreatedAsAdmin_DeniedToStorefrontKey()
        => Guard(ApiKeyScope.Storefront).CanAccessTool("unknown_tool", bearerSatisfiesMcpAccess: true)
            .Should().BeFalse();

    [Fact]
    public void IsAdminAuthorized_RequiresMcpScopeAndBearer()
    {
        Guard(ApiKeyScope.Mcp).IsAdminAuthorized(true).Should().BeTrue();
        Guard(ApiKeyScope.Mcp).IsAdminAuthorized(false).Should().BeFalse();
        Guard(ApiKeyScope.Storefront).IsAdminAuthorized(true).Should().BeFalse();
        Guard(ApiKeyScope.Unspecified).IsAdminAuthorized(true).Should().BeFalse();
    }
}
