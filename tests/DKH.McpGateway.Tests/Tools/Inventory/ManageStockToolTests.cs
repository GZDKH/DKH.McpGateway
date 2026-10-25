using DKH.McpGateway.Application.Tools.Inventory;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class ManageStockToolTests
{
    [Theory]
    [InlineData("set_level")]
    [InlineData("adjust")]
    [InlineData("get_warehouse_stock")]
    public async Task UnsupportedLegacyAction_IsExplicitlyRetiredAsync(string action)
    {
        var result = await ManageStockTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), action);
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("retired").And.Contain("exact stock");
    }

    [Fact]
    public async Task Unauthenticated_IsStillDeniedAsync()
    {
        await ((Func<Task>)(async () => await ManageStockTool.ExecuteAsync(ApiKeyContextMocks.Unauthenticated(), "set_level"))).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task WritePermission_IsStillRequiredAsync()
    {
        await ((Func<Task>)(async () => await ManageStockTool.ExecuteAsync(ApiKeyContextMocks.ReadOnly(), "set_level"))).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Cancellation_IsNotSuccessAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await ((Func<Task>)(async () => await ManageStockTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), "set_level", cancellation.Token))).Should().ThrowAsync<OperationCanceledException>();
    }
}
