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

    [Fact]
    public async Task GetLevel_HappyPath_ReturnsStockLevelAsync()
    {
        var productId = Guid.NewGuid().ToString();
        _client.GetStockLevelAsync(
                Arg.Any<GetStockLevelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new StockLevelModel
            {
                ProductId = new GuidValue(productId),
                QuantityOnHand = 50,
                QuantityAvailable = 45,
                ReorderLevel = 10,
            }));

        var result = await QueryStockTool.ExecuteAsync(_auth, _client, "get_level", productId: productId);

        result.Should().Contain(productId);
    }

    [Fact]
    public async Task GetLevel_MissingProductId_ReturnsErrorAsync()
    {
        var result = await QueryStockTool.ExecuteAsync(_auth, _client, "get_level");

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task CheckAvailability_HappyPath_ReturnsAvailabilityAsync()
    {
        var productId = Guid.NewGuid().ToString();
        var warehouseId = Guid.NewGuid().ToString();

        _client.CheckAvailabilityAsync(
                Arg.Any<CheckAvailabilityRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CheckAvailabilityResponse
            {
                IsAvailable = true,
                QuantityAvailable = 100,
            }));

        var result = await QueryStockTool.ExecuteAsync(
            _auth, _client, "check_availability", productId: productId, warehouseId: warehouseId, quantity: 10);

        var json = Parse(result);
        json.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
        json.GetProperty("quantityAvailable").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task CheckAvailability_MissingProductId_ReturnsErrorAsync()
    {
        var result = await QueryStockTool.ExecuteAsync(
            _auth, _client, "check_availability", warehouseId: Guid.NewGuid().ToString(), quantity: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task CheckAvailability_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await QueryStockTool.ExecuteAsync(
            _auth, _client, "check_availability", productId: Guid.NewGuid().ToString(), quantity: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task CheckAvailability_InvalidQuantity_ReturnsErrorAsync()
    {
        var result = await QueryStockTool.ExecuteAsync(
            _auth, _client, "check_availability",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString(), quantity: 0);

        Parse(result).GetProperty("error").GetString().Should().Contain("quantity must be a positive integer");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await QueryStockTool.ExecuteAsync(_auth, _client, "invalid_action");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => QueryStockTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, "get_level", productId: Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
