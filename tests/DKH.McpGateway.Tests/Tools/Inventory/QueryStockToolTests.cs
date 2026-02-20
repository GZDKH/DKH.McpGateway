using DKH.InventoryService.Contracts.Inventory.Api.StockQuery.v1;
using DKH.InventoryService.Contracts.Inventory.Models.Stock.v1;
using DKH.McpGateway.Application.Tools.Inventory;
using DKH.Platform.Grpc.Common.Types;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public class QueryStockToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StockQueryService.StockQueryServiceClient _client =
        Substitute.For<StockQueryService.StockQueryServiceClient>();

    private static readonly string ProductId = Guid.NewGuid().ToString();
    private static readonly string VariantId = Guid.NewGuid().ToString();
    private static readonly string WarehouseId = Guid.NewGuid().ToString();

    [Fact]
    public async Task GetLevel_HappyPath_ReturnsStockLevelAsync()
    {
        SetupGetStockLevel(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            QuantityOnHand = 50,
            QuantityAvailable = 45,
            ReorderLevel = 10,
        });

        var result = await ExecuteToolAsync("get_level", productId: ProductId);

        result.Should().Contain(ProductId);
        result.Should().Contain("45");
    }

    [Fact]
    public async Task GetLevel_WithVariantId_SetsVariantOnRequestAsync()
    {
        SetupGetStockLevel(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            QuantityAvailable = 50,
        });

        await ExecuteToolAsync("get_level", productId: ProductId, variantId: VariantId, warehouseId: WarehouseId);

        _ = _client.Received(1).GetStockLevelAsync(
            Arg.Is<GetStockLevelRequest>(r =>
                r.ProductId.Value == ProductId &&
                r.VariantId.Value == VariantId),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLevel_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("get_level");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task GetLevels_HappyPath_ReturnsItemsAndCountAsync()
    {
        var response = new GetStockLevelsResponse();
        response.Items.Add(new StockLevelModel
        {
            StockId = new GuidValue(Guid.NewGuid().ToString()),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            QuantityAvailable = 42,
        });
        SetupGetStockLevels(response);

        var result = await ExecuteToolAsync("get_levels",
            json: /*lang=json,strict*/ "{\"items\":[{\"productId\":{\"value\":\"" + ProductId + "\"}}]}");

        var parsed = Parse(result);
        parsed.GetProperty("count").GetInt32().Should().Be(1);
        parsed.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetLevels_MissingJson_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("get_levels");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("json is required");
    }

    [Fact]
    public async Task CheckAvailability_HappyPath_ReturnsAvailabilityAsync()
    {
        SetupCheckAvailability(new CheckAvailabilityResponse
        {
            IsAvailable = true,
            QuantityAvailable = 100,
        });

        var result = await ExecuteToolAsync("check_availability",
            productId: ProductId, warehouseId: WarehouseId, quantity: 10);

        var json = Parse(result);
        json.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
        json.GetProperty("quantityAvailable").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task CheckAvailability_NotAvailable_ReturnsFalseAsync()
    {
        SetupCheckAvailability(new CheckAvailabilityResponse
        {
            IsAvailable = false,
            QuantityAvailable = 3,
        });

        var result = await ExecuteToolAsync("check_availability",
            productId: ProductId, warehouseId: WarehouseId, quantity: 10);

        var json = Parse(result);
        json.GetProperty("isAvailable").GetBoolean().Should().BeFalse();
        json.GetProperty("quantityAvailable").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task CheckAvailability_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("check_availability",
            warehouseId: WarehouseId, quantity: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task CheckAvailability_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("check_availability",
            productId: ProductId, quantity: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task CheckAvailability_InvalidQuantity_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("check_availability",
            productId: ProductId, warehouseId: WarehouseId, quantity: 0);

        Parse(result).GetProperty("error").GetString().Should().Contain("quantity must be a positive integer");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("invalid_action");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Theory]
    [InlineData("GET_LEVEL")]
    [InlineData("Get_Level")]
    [InlineData("CHECK_AVAILABILITY")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupGetStockLevel(new StockLevelModel
        {
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
        });
        SetupCheckAvailability(new CheckAvailabilityResponse { IsAvailable = true, QuantityAvailable = 10 });

        var result = await ExecuteToolAsync(action,
            productId: ProductId, warehouseId: WarehouseId, quantity: 1);

        result.Should().NotContain("Unknown action");
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => QueryStockTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, "get_level", productId: ProductId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetStockLevelAsync(
                Arg.Any<GetStockLevelRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<StockLevelModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("get_level", productId: ProductId);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action,
        string? productId = null,
        string? variantId = null,
        string? warehouseId = null,
        int? quantity = null,
        string? json = null)
        => QueryStockTool.ExecuteAsync(
            _auth, _client,
            action: action, productId: productId, variantId: variantId,
            warehouseId: warehouseId, quantity: quantity, json: json);

    private void SetupGetStockLevel(StockLevelModel response)
        => _client.GetStockLevelAsync(
                Arg.Any<GetStockLevelRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupGetStockLevels(GetStockLevelsResponse response)
        => _client.GetStockLevelsAsync(
                Arg.Any<GetStockLevelsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupCheckAvailability(CheckAvailabilityResponse response)
        => _client.CheckAvailabilityAsync(
                Arg.Any<CheckAvailabilityRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
