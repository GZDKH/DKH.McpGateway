using DKH.McpGateway.Application.Tools.Inventory;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class ManageStockAlertToolTests
{
    [Theory]
    [InlineData("list")]
    [InlineData("configure")]
    [InlineData("acknowledge")]
    public async Task UnsupportedLegacyAction_IsExplicitlyRetiredAsync(string action)
    {
        var result = await ManageStockAlertTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), action);
        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("retired").And.Contain("exact stock");
    }

    [Fact]
    public async Task Unauthenticated_IsStillDeniedAsync()
    {
        await ((Func<Task>)(async () => await ManageStockAlertTool.ExecuteAsync(ApiKeyContextMocks.Unauthenticated(), "list"))).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task WritePermission_IsStillRequiredAsync()
    {
        await ((Func<Task>)(async () => await ManageStockAlertTool.ExecuteAsync(ApiKeyContextMocks.ReadOnly(), "configure"))).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Cancellation_IsNotSuccessAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await ((Func<Task>)(async () => await ManageStockAlertTool.ExecuteAsync(ApiKeyContextMocks.FullAccess(), "list", cancellation.Token))).Should().ThrowAsync<OperationCanceledException>();
    }
}
