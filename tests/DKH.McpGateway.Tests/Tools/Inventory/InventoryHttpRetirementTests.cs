using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class InventoryHttpRetirementTests
{
    [Theory]
    [InlineData("manage_stock", "adjust")]
    [InlineData("manage_stock_alert", "configure")]
    public async Task RetiredTools_ExecuteThroughRealMcpTransportWithoutAnyInventoryClientAsync(string tool, string action)
    {
        // The existing host has no Inventory client registered. It exercises real
        // MCP binding/DI/auth with controlled API-key/principal inputs, not native grants.
        await using var server = await McpHttpTestServer.StartAsync(s => s.WithKey(
            "controlled-stock-mcp-key", ApiKeyScope.Mcp, null, McpPermissions.Read, McpPermissions.Write));
        var session = await server.ConnectAsync("controlled-stock-mcp-key", withAdminBearer: true);
        (await session.ListToolNamesAsync()).Should().Contain(["query_stock", "manage_reservation", "manage_stock", "manage_stock_alert"]);
        var outcome = await session.CallToolAsync(tool, new Dictionary<string, object?> { ["action"] = action });
        outcome.IsError.Should().BeFalse();
        var json = JsonDocument.Parse(outcome.Text).RootElement;
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("retired");
    }

    [Fact]
    public async Task StorefrontKey_CannotDiscoverOrInvokeStockToolsAsync()
    {
        await using var server = await McpHttpTestServer.StartAsync(s => s.WithKey(
            "controlled-storefront-key", ApiKeyScope.Storefront, Guid.NewGuid()));
        var session = await server.ConnectAsync("controlled-storefront-key");
        (await session.ListToolNamesAsync()).Should().NotContain(["query_stock", "manage_reservation", "manage_stock", "manage_stock_alert"]);
        var outcome = await session.CallToolAsync("manage_stock", new Dictionary<string, object?> { ["action"] = "adjust" });
        outcome.IsError.Should().BeTrue();
    }
}
