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

    private static readonly string ProductId = Guid.NewGuid().ToString();
    private static readonly string WarehouseId = Guid.NewGuid().ToString();
    private static readonly string VariantId = Guid.NewGuid().ToString();

    [Fact]
    public async Task SetLevel_HappyPath_ReturnsStockLevelAsync()
    {
        SetupSetStockLevel(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            QuantityOnHand = 100,
        });

        var result = await ExecuteToolAsync("set_level",
            productId: ProductId, warehouseId: WarehouseId, quantity: 100);

        result.Should().Contain(ProductId);
    }

    [Fact]
    public async Task SetLevel_WithVariantAndReorderLevel_SetsAllFieldsAsync()
    {
        SetupSetStockLevel(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            QuantityOnHand = 50,
            ReorderLevel = 10,
        });

        await ExecuteToolAsync("set_level",
            productId: ProductId, variantId: VariantId, warehouseId: WarehouseId,
            quantity: 50, reorderLevel: 10);

        _ = _client.Received(1).SetStockLevelAsync(
            Arg.Is<SetStockLevelRequest>(r =>
                r.ProductId.Value == ProductId &&
                r.VariantId.Value == VariantId &&
                r.WarehouseId.Value == WarehouseId &&
                r.Quantity == 50 &&
                r.ReorderLevel == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetLevel_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("set_level",
            warehouseId: WarehouseId, quantity: 100);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task SetLevel_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("set_level",
            productId: ProductId, quantity: 100);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task SetLevel_MissingQuantity_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("set_level",
            productId: ProductId, warehouseId: WarehouseId);

        Parse(result).GetProperty("error").GetString().Should().Contain("quantity is required");
    }

    [Fact]
    public async Task SetLevel_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStockTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "set_level",
            productId: ProductId, warehouseId: WarehouseId, quantity: 100);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Adjust_HappyPath_ReturnsUpdatedStockAsync()
    {
        SetupAdjustStock(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            QuantityOnHand = 95,
        });

        var result = await ExecuteToolAsync("adjust",
            productId: ProductId, warehouseId: WarehouseId, adjustment: -5, reason: "Damaged");

        result.Should().Contain(ProductId);
    }

    [Fact]
    public async Task Adjust_WithVariantAndReason_SetsAllFieldsAsync()
    {
        SetupAdjustStock(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            QuantityOnHand = 45,
        });

        await ExecuteToolAsync("adjust",
            productId: ProductId, variantId: VariantId, warehouseId: WarehouseId,
            adjustment: -5, reason: "Quality issue");

        _ = _client.Received(1).AdjustStockAsync(
            Arg.Is<AdjustStockRequest>(r =>
                r.ProductId.Value == ProductId &&
                r.VariantId.Value == VariantId &&
                r.Adjustment == -5 &&
                r.Reason == "Quality issue"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adjust_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("adjust",
            warehouseId: WarehouseId, adjustment: -5);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task Adjust_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("adjust",
            productId: ProductId, adjustment: -5);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task Adjust_MissingAdjustment_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("adjust",
            productId: ProductId, warehouseId: WarehouseId);

        Parse(result).GetProperty("error").GetString().Should().Contain("adjustment is required");
    }

    [Fact]
    public async Task Adjust_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStockTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "adjust",
            productId: ProductId, warehouseId: WarehouseId, adjustment: -5);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetWarehouseStock_HappyPath_ReturnsPagedResultAsync()
    {
        SetupGetWarehouseStock(new GetWarehouseStockResponse
        {
            Items =
            {
                new StockLevelModel
                {
                    StockId = new GuidValue(Guid.NewGuid().ToString()),
                    ProductId = new GuidValue(ProductId),
                    WarehouseId = new GuidValue(WarehouseId),
                    QuantityOnHand = 100,
                    QuantityAvailable = 90,
                },
            },
            Metadata = new PaginationMetadata { TotalCount = 1, CurrentPage = 1, PageSize = 20 },
        });

        var result = await ExecuteToolAsync("get_warehouse_stock", warehouseId: WarehouseId);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetWarehouseStock_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("get_warehouse_stock");

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task GetWarehouseStock_WithPagination_SetsPaginationFieldsAsync()
    {
        SetupGetWarehouseStock(new GetWarehouseStockResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 3, PageSize = 10 },
        });

        await ExecuteToolAsync("get_warehouse_stock", warehouseId: WarehouseId, page: 3, pageSize: 10);

        _ = _client.Received(1).GetWarehouseStockAsync(
            Arg.Is<GetWarehouseStockRequest>(r =>
                r.Pagination.Page == 3 &&
                r.Pagination.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("unknown");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Theory]
    [InlineData("SET_LEVEL")]
    [InlineData("Set_Level")]
    [InlineData("ADJUST")]
    [InlineData("GET_WAREHOUSE_STOCK")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupSetStockLevel(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
        });
        SetupAdjustStock(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
        });
        SetupGetWarehouseStock(new GetWarehouseStockResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
        });

        var result = await ExecuteToolAsync(action,
            productId: ProductId, warehouseId: WarehouseId,
            quantity: 10, adjustment: 1);

        result.Should().NotContain("Unknown action");
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.SetStockLevelAsync(
                Arg.Any<SetStockLevelRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<StockLevelModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("set_level",
            productId: ProductId, warehouseId: WarehouseId, quantity: 10);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action,
        string? productId = null,
        string? variantId = null,
        string? warehouseId = null,
        int? quantity = null,
        int? reorderLevel = null,
        int? adjustment = null,
        string? reason = null,
        int? page = null,
        int? pageSize = null)
        => ManageStockTool.ExecuteAsync(
            _auth, _client,
            action: action, productId: productId, variantId: variantId,
            warehouseId: warehouseId, quantity: quantity, reorderLevel: reorderLevel,
            adjustment: adjustment, reason: reason, page: page, pageSize: pageSize);

    private void SetupSetStockLevel(StockLevelModel response)
        => _client.SetStockLevelAsync(
                Arg.Any<SetStockLevelRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupAdjustStock(StockLevelModel response)
        => _client.AdjustStockAsync(
                Arg.Any<AdjustStockRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGetWarehouseStock(GetWarehouseStockResponse response)
        => _client.GetWarehouseStockAsync(
                Arg.Any<GetWarehouseStockRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
