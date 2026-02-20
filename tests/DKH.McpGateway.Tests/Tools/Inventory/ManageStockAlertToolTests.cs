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

    private static readonly string ProductId = Guid.NewGuid().ToString();
    private static readonly string WarehouseId = Guid.NewGuid().ToString();
    private static readonly string VariantId = Guid.NewGuid().ToString();
    private static readonly string AlertId = Guid.NewGuid().ToString();

    [Fact]
    public async Task List_HappyPath_ReturnsAlertsAsync()
    {
        SetupGetAlerts(new GetAlertsResponse
        {
            Items =
            {
                new LowStockAlertModel
                {
                    AlertId = new GuidValue(AlertId),
                    ProductId = new GuidValue(ProductId),
                    WarehouseId = new GuidValue(WarehouseId),
                    CurrentQuantity = 3,
                    ReorderLevel = 10,
                    Acknowledged = false,
                    DetectedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                },
            },
            Metadata = new PaginationMetadata { TotalCount = 1, CurrentPage = 1, PageSize = 20 },
        });

        var result = await ExecuteToolAsync("list");

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task List_WithFilters_SetsFilterFieldsAsync()
    {
        SetupGetAlerts(new GetAlertsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 2, PageSize = 10 },
        });

        await ExecuteToolAsync("list", warehouseId: WarehouseId, acknowledged: false, page: 2, pageSize: 10);

        _ = _client.Received(1).GetAlertsAsync(
            Arg.Is<GetAlertsRequest>(r =>
                r.WarehouseId.Value == WarehouseId &&
                !r.Acknowledged &&
                r.Pagination.Page == 2 &&
                r.Pagination.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Configure_HappyPath_ReturnsAlertConfigAsync()
    {
        SetupConfigureAlert(new LowStockAlertModel
        {
            AlertId = new GuidValue(AlertId),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            ReorderLevel = 15,
            DetectedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
        });

        var result = await ExecuteToolAsync("configure",
            productId: ProductId, warehouseId: WarehouseId, reorderLevel: 15);

        result.Should().Contain(ProductId);
    }

    [Fact]
    public async Task Configure_WithVariantId_SetsVariantOnRequestAsync()
    {
        SetupConfigureAlert(new LowStockAlertModel
        {
            AlertId = new GuidValue(AlertId),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            ReorderLevel = 15,
            DetectedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
        });

        await ExecuteToolAsync("configure",
            productId: ProductId, variantId: VariantId, warehouseId: WarehouseId, reorderLevel: 15);

        _ = _client.Received(1).ConfigureAlertAsync(
            Arg.Is<ConfigureAlertRequest>(r =>
                r.ProductId.Value == ProductId &&
                r.VariantId.Value == VariantId &&
                r.WarehouseId.Value == WarehouseId &&
                r.ReorderLevel == 15),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Configure_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("configure",
            warehouseId: WarehouseId, reorderLevel: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task Configure_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("configure",
            productId: ProductId, reorderLevel: 10);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task Configure_MissingReorderLevel_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("configure",
            productId: ProductId, warehouseId: WarehouseId);

        Parse(result).GetProperty("error").GetString().Should().Contain("reorderLevel is required");
    }

    [Fact]
    public async Task Configure_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStockAlertTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "configure",
            productId: ProductId, warehouseId: WarehouseId, reorderLevel: 10);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Acknowledge_HappyPath_ReturnsOkAsync()
    {
        SetupAcknowledgeAlert();

        var result = await ExecuteToolAsync("acknowledge", alertId: AlertId);

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Acknowledge_MissingAlertId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("acknowledge");

        Parse(result).GetProperty("error").GetString().Should().Contain("alertId is required");
    }

    [Fact]
    public async Task Acknowledge_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageStockAlertTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "acknowledge",
            alertId: AlertId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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
    [InlineData("LIST")]
    [InlineData("List")]
    [InlineData("CONFIGURE")]
    [InlineData("ACKNOWLEDGE")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupGetAlerts(new GetAlertsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
        });
        SetupConfigureAlert(new LowStockAlertModel
        {
            AlertId = new GuidValue(AlertId),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            ReorderLevel = 10,
            DetectedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
        });
        SetupAcknowledgeAlert();

        var result = await ExecuteToolAsync(action,
            alertId: AlertId, productId: ProductId, warehouseId: WarehouseId, reorderLevel: 10);

        result.Should().NotContain("Unknown action");
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetAlertsAsync(
                Arg.Any<GetAlertsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<GetAlertsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("list");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action,
        string? alertId = null,
        string? productId = null,
        string? variantId = null,
        string? warehouseId = null,
        int? reorderLevel = null,
        bool? acknowledged = null,
        int? page = null,
        int? pageSize = null)
        => ManageStockAlertTool.ExecuteAsync(
            _auth, _client,
            action: action, alertId: alertId, productId: productId, variantId: variantId,
            warehouseId: warehouseId, reorderLevel: reorderLevel, acknowledged: acknowledged,
            page: page, pageSize: pageSize);

    private void SetupGetAlerts(GetAlertsResponse response)
        => _client.GetAlertsAsync(
                Arg.Any<GetAlertsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupConfigureAlert(LowStockAlertModel response)
        => _client.ConfigureAlertAsync(
                Arg.Any<ConfigureAlertRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupAcknowledgeAlert()
        => _client.AcknowledgeAlertAsync(
                Arg.Any<AcknowledgeAlertRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
