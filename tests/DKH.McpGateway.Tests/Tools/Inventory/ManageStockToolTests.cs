using DKH.InventoryService.Contracts.Inventory.Api.StockManagement.v1;
using DKH.InventoryService.Contracts.Inventory.Models.Stock.v1;
using DKH.McpGateway.Application.Tools.Inventory;
using DKH.Platform.Grpc.Common.Types;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public class ManageStockToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StockManagementService.StockManagementServiceClient _client =
        Substitute.For<StockManagementService.StockManagementServiceClient>();

    [Fact]
    public async Task SetLevel_HappyPath_ReturnsStockLevelAsync()
    {
        var productId = Guid.NewGuid().ToString();
        var warehouseId = Guid.NewGuid().ToString();

        _client.SetStockLevelAsync(
                Arg.Any<SetStockLevelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new StockLevelModel
            {
                ProductId = new GuidValue(productId),
                WarehouseId = new GuidValue(warehouseId),
                QuantityOnHand = 100,
            }));

        var result = await ManageStockTool.ExecuteAsync(
            _auth, _client, "set_level", productId: productId, warehouseId: warehouseId, quantity: 100);

        result.Should().Contain(productId);
    }

    [Fact]
    public async Task SetLevel_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ManageStockTool.ExecuteAsync(
            _auth, _client, "set_level", warehouseId: Guid.NewGuid().ToString(), quantity: 100);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task SetLevel_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ManageStockTool.ExecuteAsync(
            _auth, _client, "set_level", productId: Guid.NewGuid().ToString(), quantity: 100);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task SetLevel_MissingQuantity_ReturnsErrorAsync()
    {
        var result = await ManageStockTool.ExecuteAsync(
            _auth, _client, "set_level",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString());

        Parse(result).GetProperty("error").GetString().Should().Contain("quantity is required");
    }

    [Fact]
    public async Task Adjust_HappyPath_ReturnsUpdatedStockAsync()
    {
        var productId = Guid.NewGuid().ToString();
        var warehouseId = Guid.NewGuid().ToString();

        _client.AdjustStockAsync(
                Arg.Any<AdjustStockRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new StockLevelModel
            {
                ProductId = new GuidValue(productId),
                QuantityOnHand = 95,
            }));

        var result = await ManageStockTool.ExecuteAsync(
            _auth, _client, "adjust",
            productId: productId, warehouseId: warehouseId, adjustment: -5, reason: "Damaged");

        result.Should().Contain(productId);
    }

    [Fact]
    public async Task Adjust_MissingAdjustment_ReturnsErrorAsync()
    {
        var result = await ManageStockTool.ExecuteAsync(
            _auth, _client, "adjust",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString());

        Parse(result).GetProperty("error").GetString().Should().Contain("adjustment is required");
    }

    [Fact]
    public async Task GetWarehouseStock_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ManageStockTool.ExecuteAsync(_auth, _client, "get_warehouse_stock");

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageStockTool.ExecuteAsync(_auth, _client, "unknown");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task SetLevel_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStockTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "set_level",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString(), quantity: 100);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
