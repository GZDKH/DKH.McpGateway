using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;

namespace DKH.McpGateway.Tests.Auth;

/// <summary>
/// Real HTTP-protocol credential matrix for issue #86: a storefront-scoped key reaches only the
/// public storefront tools without a privileged bearer, while admin tools, prompts, and resources
/// stay hidden and denied unless an MCP key is paired with a privileged bearer. Invalid, revoked,
/// unbound, and McpDisabled keys fail closed.
/// </summary>
public sealed class McpHttpSurfaceAuthorizationTests
{
    private const string StorefrontKey = "sf-key";
    private const string McpKey = "mcp-key";
    private static readonly Guid StorefrontId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly string[] PublicToolNames =
    [
        "storefront_list_catalogs",
        "storefront_get_category_tree",
        "storefront_get_product",
        "storefront_search_products",
        "storefront_recommend_products",
    ];

    // --- Ingress: fail closed on missing/invalid credentials ---

    [Fact]
    public async Task Initialize_WithoutApiKey_IsRejectedAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync();

        var connect = async () => await server.ConnectAsync(apiKey: null);

        await connect.Should().ThrowAsync<McpHttpRejectedException>();
    }

    [Fact]
    public async Task Initialize_WithInvalidApiKey_IsRejectedAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync();

        var connect = async () => await server.ConnectAsync("unknown-key");

        await connect.Should().ThrowAsync<McpHttpRejectedException>();
    }

    // --- Storefront key: only the public storefront surface ---

    [Fact]
    public async Task StorefrontKey_ListTools_ReturnsOnlyPublicStorefrontToolsAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(StorefrontKey, ApiKeyScope.Storefront, StorefrontId));
        var session = await server.ConnectAsync(StorefrontKey);

        var tools = await session.ListToolNamesAsync();

        tools.Should().BeEquivalentTo(PublicToolNames);
    }

    [Fact]
    public async Task StorefrontKey_ListPromptsAndResources_AreEmptyAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(StorefrontKey, ApiKeyScope.Storefront, StorefrontId));
        var session = await server.ConnectAsync(StorefrontKey);

        (await session.ListPromptNamesAsync()).Should().BeEmpty();
        (await session.ListResourceNamesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task StorefrontKey_CanInvoke_PublicToolAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(StorefrontKey, ApiKeyScope.Storefront, StorefrontId).WithMcpEnabled(true));
        var session = await server.ConnectAsync(StorefrontKey);

        var outcome = await session.CallToolAsync("storefront_list_catalogs");

        outcome.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task StorefrontKey_CannotInvoke_AdminTool_ByNameAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(StorefrontKey, ApiKeyScope.Storefront, StorefrontId));
        var session = await server.ConnectAsync(StorefrontKey);

        // The surface filter denies the admin tool before it runs; the block surfaces as a
        // fail-closed error result carrying the guard's message (no downstream call is made).
        var outcome = await session.CallToolAsync(
            "search_products",
            new Dictionary<string, object?> { ["query"] = "tea" });

        outcome.IsError.Should().BeTrue();
        outcome.Text.Should().Contain("not available for the presented credential");
    }

    // --- Storefront key fail-closed at the tool guard ---

    [Fact]
    public async Task McpDisabledStorefrontKey_InvokePublicTool_FailsClosedAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(StorefrontKey, ApiKeyScope.Storefront, StorefrontId).WithMcpEnabled(false));
        var session = await server.ConnectAsync(StorefrontKey);

        var outcome = await session.CallToolAsync("storefront_list_catalogs");

        outcome.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task UnboundStorefrontKey_InvokePublicTool_FailsClosedAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(StorefrontKey, ApiKeyScope.Storefront, storefrontId: null));
        var session = await server.ConnectAsync(StorefrontKey);

        var outcome = await session.CallToolAsync("storefront_list_catalogs");

        outcome.IsError.Should().BeTrue();
    }

    // --- MCP admin key: admin surface requires the privileged bearer ---

    [Fact]
    public async Task McpKey_WithBearer_ListTools_IncludesAdminAndPublicAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(McpKey, ApiKeyScope.Mcp, storefrontId: null, "mcp:read", "mcp:write"));
        var session = await server.ConnectAsync(McpKey, withAdminBearer: true);

        var names = await session.ListToolNamesAsync();

        names.Should().Contain("search_products");
        names.Should().Contain(PublicToolNames);
    }

    [Fact]
    public async Task McpKey_WithoutBearer_ListTools_HidesAdminToolsAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(McpKey, ApiKeyScope.Mcp, storefrontId: null, "mcp:read", "mcp:write"));
        var session = await server.ConnectAsync(McpKey, withAdminBearer: false);

        var names = await session.ListToolNamesAsync();

        names.Should().NotContain("search_products");
    }

    [Fact]
    public async Task McpKey_WithoutBearer_ListPromptsAndResources_AreEmptyAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(McpKey, ApiKeyScope.Mcp, storefrontId: null, "mcp:read"));
        var session = await server.ConnectAsync(McpKey, withAdminBearer: false);

        (await session.ListPromptNamesAsync()).Should().BeEmpty();
        (await session.ListResourceNamesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task McpKey_WithBearer_SeesAdminPromptsAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s =>
            s.WithKey(McpKey, ApiKeyScope.Mcp, storefrontId: null, "mcp:read"));
        var session = await server.ConnectAsync(McpKey, withAdminBearer: true);

        // Admin prompts become visible with a privileged bearer (surface authorization passed).
        (await session.ListPromptNamesAsync()).Should().NotBeEmpty();
    }
}
