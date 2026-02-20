using DKH.InventoryService.Contracts.Inventory.Api.LowStockAlert.v1;
using DKH.McpGateway.Application.Tools.Inventory;
using DKH.Platform.Grpc.Common.Types;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public class ManageStockAlertToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly LowStockAlertService.LowStockAlertServiceClient _client =
        Substitute.For<LowStockAlertService.LowStockAlertServiceClient>();

    [Fact]
    public async Task List_HappyPath_ReturnsAlertsAsync()
    {
        _client.GetAlertsAsync(
                Arg.Any<GetAlertsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetAlertsResponse
            {
                Items =
                {
                    new LowStockAlertModel
                    {
                        AlertId = new GuidValue(Guid.NewGuid().ToString()),
                        ProductId = new GuidValue(Guid.NewGuid().ToString()),
                        WarehouseId = new GuidValue(Guid.NewGuid().ToString()),
                        CurrentQuantity = 3,
                        ReorderLevel = 10,
                        Acknowledged = false,
                        DetectedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                    },
                },
                Metadata = new PaginationMetadata { TotalCount = 1, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ManageStockAlertTool.ExecuteAsync(_auth, _client, "list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Configure_HappyPath_ReturnsAlertConfigAsync()
    {
        var productId = Guid.NewGuid().ToString();
        var warehouseId = Guid.NewGuid().ToString();

        _client.ConfigureAlertAsync(
                Arg.Any<ConfigureAlertRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new LowStockAlertModel
            {
                AlertId = new GuidValue(Guid.NewGuid().ToString()),
                ProductId = new GuidValue(productId),
                WarehouseId = new GuidValue(warehouseId),
                ReorderLevel = 15,
                DetectedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            }));

        var result = await ManageStockAlertTool.ExecuteAsync(
            _auth, _client, "configure", productId: productId, warehouseId: warehouseId, reorderLevel: 15);

        result.Should().Contain(productId);
    }

    [Fact]
    public async Task Configure_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ManageStockAlertTool.ExecuteAsync(
            _auth, _client, "configure", warehouseId: Guid.NewGuid().ToString(), reorderLevel: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task Configure_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ManageStockAlertTool.ExecuteAsync(
            _auth, _client, "configure", productId: Guid.NewGuid().ToString(), reorderLevel: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task Configure_MissingReorderLevel_ReturnsErrorAsync()
    {
        var result = await ManageStockAlertTool.ExecuteAsync(
            _auth, _client, "configure",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString());

        Parse(result).GetProperty("error").GetString().Should().Contain("reorderLevel is required");
    }

    [Fact]
    public async Task Acknowledge_MissingAlertId_ReturnsErrorAsync()
    {
        var result = await ManageStockAlertTool.ExecuteAsync(_auth, _client, "acknowledge");

        Parse(result).GetProperty("error").GetString().Should().Contain("alertId is required");
    }

    [Fact]
    public async Task Acknowledge_HappyPath_ReturnsOkAsync()
    {
        _client.AcknowledgeAlertAsync(
                Arg.Any<AcknowledgeAlertRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

        var result = await ManageStockAlertTool.ExecuteAsync(
            _auth, _client, "acknowledge", alertId: Guid.NewGuid().ToString());

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageStockAlertTool.ExecuteAsync(_auth, _client, "unknown");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Configure_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStockAlertTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "configure",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString(), reorderLevel: 10);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
