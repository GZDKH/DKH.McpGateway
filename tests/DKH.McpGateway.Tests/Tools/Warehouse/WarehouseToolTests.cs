using DKH.McpGateway.Application.Tools.HandlingTasks;
using DKH.McpGateway.Application.Tools.IncomingShipments;
using DKH.McpGateway.Application.Tools.TransferOrders;
using DKH.McpGateway.Application.Tools.Warehouse;
using DKH.McpGateway.Application.Tools.WarehouseZones;
using DKH.Platform.Grpc.Common.Types;
using DKH.WarehouseService.Contracts.Warehouse.Api.HandlingTask.v1;
using DKH.WarehouseService.Contracts.Warehouse.Api.IncomingShipment.v1;
using DKH.WarehouseService.Contracts.Warehouse.Api.TransferOrder.v1;
using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Api.WarehouseZoneCrud.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Tests.Tools.Warehouse;

public class WarehouseToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly IApiKeyContext _readOnly = ApiKeyContextMocks.ReadOnly();

    private readonly WarehouseCrudService.WarehouseCrudServiceClient _warehouse =
        Substitute.For<WarehouseCrudService.WarehouseCrudServiceClient>();

    private readonly WarehouseZoneCrudService.WarehouseZoneCrudServiceClient _zone =
        Substitute.For<WarehouseZoneCrudService.WarehouseZoneCrudServiceClient>();

    private readonly HandlingTaskService.HandlingTaskServiceClient _task =
        Substitute.For<HandlingTaskService.HandlingTaskServiceClient>();

    private readonly IncomingShipmentService.IncomingShipmentServiceClient _shipment =
        Substitute.For<IncomingShipmentService.IncomingShipmentServiceClient>();

    private readonly TransferOrderService.TransferOrderServiceClient _transfer =
        Substitute.For<TransferOrderService.TransferOrderServiceClient>();

    private static readonly string WarehouseId = Guid.NewGuid().ToString();
    private static readonly string ZoneId = Guid.NewGuid().ToString();
    private static readonly string TaskId = Guid.NewGuid().ToString();
    private static readonly string ShipmentId = Guid.NewGuid().ToString();
    private static readonly string OrderId = Guid.NewGuid().ToString();
    private static readonly string TenantId = Guid.NewGuid().ToString();

    // ---- GetWarehouse ----

    [Fact]
    public async Task GetWarehouse_MissingId_ReturnsErrorAsync()
    {
        var result = await GetWarehouseTool.ExecuteAsync(_auth, _warehouse, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetWarehouse_HappyPath_ReturnsWarehouseAsync()
    {
        _warehouse.GetWarehouseAsync(
                Arg.Any<GetWarehouseRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new WarehouseModel
            {
                Id = new GuidValue(WarehouseId),
                TenantId = new GuidValue(TenantId),
                Code = "WH-001",
                OwnershipType = WarehouseOwnershipType.Own,
                Status = WarehouseStatus.Active,
                Timezone = "Europe/Moscow",
            }));

        var result = await GetWarehouseTool.ExecuteAsync(_auth, _warehouse, id: WarehouseId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(WarehouseId);
        json.GetProperty("code").GetString().Should().Be("WH-001");
        json.GetProperty("ownershipType").GetString().Should().Be("Own");
        json.GetProperty("status").GetString().Should().Be("Active");
    }

    // ---- ListWarehouses ----

    [Fact]
    public async Task ListWarehouses_HappyPath_ReturnsPaginatedAsync()
    {
        _warehouse.ListWarehousesAsync(
                Arg.Any<ListWarehousesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListWarehousesResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListWarehousesTool.ExecuteAsync(_auth, _warehouse);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- CreateWarehouse (write) ----

    [Fact]
    public async Task CreateWarehouse_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateWarehouseTool.ExecuteAsync(
            _readOnly, _warehouse,
            tenantId: TenantId, code: "WH-001",
            displayName: /*lang=json,strict*/ "[{\"lang\":\"en\",\"name\":\"Main\"}]",
            ownershipType: "Own", timezone: "UTC");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateWarehouse_MissingCode_ReturnsErrorAsync()
    {
        var result = await CreateWarehouseTool.ExecuteAsync(
            _auth, _warehouse,
            tenantId: TenantId, code: "",
            displayName: /*lang=json,strict*/ "[{\"lang\":\"en\",\"name\":\"Main\"}]",
            ownershipType: "Own", timezone: "UTC");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    [Fact]
    public async Task CreateWarehouse_InvalidDisplayNameJson_ReturnsErrorAsync()
    {
        var result = await CreateWarehouseTool.ExecuteAsync(
            _auth, _warehouse,
            tenantId: TenantId, code: "WH-001",
            displayName: "not-json",
            ownershipType: "Own", timezone: "UTC");

        Parse(result).GetProperty("error").GetString().Should().Contain("displayName JSON is invalid");
    }

    // ---- ArchiveWarehouse (write) ----

    [Fact]
    public async Task ArchiveWarehouse_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ArchiveWarehouseTool.ExecuteAsync(_readOnly, _warehouse, id: WarehouseId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ArchiveWarehouse_MissingId_ReturnsErrorAsync()
    {
        var result = await ArchiveWarehouseTool.ExecuteAsync(_auth, _warehouse, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- SuspendWarehouse (write) ----

    [Fact]
    public async Task SuspendWarehouse_MissingId_ReturnsErrorAsync()
    {
        var result = await SuspendWarehouseTool.ExecuteAsync(_auth, _warehouse, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- ActivateWarehouse (write) ----

    [Fact]
    public async Task ActivateWarehouse_MissingId_ReturnsErrorAsync()
    {
        var result = await ActivateWarehouseTool.ExecuteAsync(_auth, _warehouse, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GetWarehouseZone ----

    [Fact]
    public async Task GetWarehouseZone_MissingId_ReturnsErrorAsync()
    {
        var result = await GetWarehouseZoneTool.ExecuteAsync(_auth, _zone, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetWarehouseZone_HappyPath_ReturnsZoneAsync()
    {
        _zone.GetWarehouseZoneAsync(
                Arg.Any<GetWarehouseZoneRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new WarehouseZoneModel
            {
                Id = new GuidValue(ZoneId),
                WarehouseId = new GuidValue(WarehouseId),
                Code = "ZONE-A",
                Type = WarehouseZoneType.Storage,
                Status = WarehouseZoneStatus.Active,
            }));

        var result = await GetWarehouseZoneTool.ExecuteAsync(_auth, _zone, id: ZoneId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(ZoneId);
        json.GetProperty("type").GetString().Should().Be("Storage");
        json.GetProperty("status").GetString().Should().Be("Active");
    }

    // ---- ListWarehouseZones ----

    [Fact]
    public async Task ListWarehouseZones_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ListWarehouseZonesTool.ExecuteAsync(_auth, _zone, warehouseId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task ListWarehouseZones_HappyPath_ReturnsItemsAsync()
    {
        _zone.ListWarehouseZonesAsync(
                Arg.Any<ListWarehouseZonesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListWarehouseZonesResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListWarehouseZonesTool.ExecuteAsync(_auth, _zone, warehouseId: WarehouseId);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- CreateWarehouseZone (write) ----

    [Fact]
    public async Task CreateWarehouseZone_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateWarehouseZoneTool.ExecuteAsync(
            _readOnly, _zone,
            warehouseId: WarehouseId, code: "ZONE-A",
            displayName: /*lang=json,strict*/ "[{\"lang\":\"en\",\"name\":\"Zone A\"}]",
            type: "Storage");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateWarehouseZone_MissingCode_ReturnsErrorAsync()
    {
        var result = await CreateWarehouseZoneTool.ExecuteAsync(
            _auth, _zone,
            warehouseId: WarehouseId, code: "",
            displayName: /*lang=json,strict*/ "[{\"lang\":\"en\",\"name\":\"Zone A\"}]",
            type: "Storage");

        Parse(result).GetProperty("error").GetString().Should().Contain("code is required");
    }

    // ---- ArchiveWarehouseZone (write) ----

    [Fact]
    public async Task ArchiveWarehouseZone_MissingId_ReturnsErrorAsync()
    {
        var result = await ArchiveWarehouseZoneTool.ExecuteAsync(_auth, _zone, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GetHandlingTask ----

    [Fact]
    public async Task GetHandlingTask_MissingId_ReturnsErrorAsync()
    {
        var result = await GetHandlingTaskTool.ExecuteAsync(_auth, _task, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetHandlingTask_HappyPath_ReturnsTaskAsync()
    {
        _task.GetHandlingTaskAsync(
                Arg.Any<GetHandlingTaskRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new HandlingTaskModel
            {
                Id = new GuidValue(TaskId),
                WarehouseId = new GuidValue(WarehouseId),
                Type = HandlingTaskType.Receiving,
                Status = HandlingTaskStatus.Pending,
                Priority = HandlingTaskPriority.Normal,
            }));

        var result = await GetHandlingTaskTool.ExecuteAsync(_auth, _task, id: TaskId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(TaskId);
        json.GetProperty("type").GetString().Should().Be("Receiving");
        json.GetProperty("status").GetString().Should().Be("Pending");
        json.GetProperty("priority").GetString().Should().Be("Normal");
    }

    // ---- ListHandlingTasks ----

    [Fact]
    public async Task ListHandlingTasks_HappyPath_ReturnsPaginatedAsync()
    {
        _task.ListHandlingTasksAsync(
                Arg.Any<ListHandlingTasksRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListHandlingTasksResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListHandlingTasksTool.ExecuteAsync(_auth, _task, warehouseId: WarehouseId);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- CreateHandlingTask (write) ----

    [Fact]
    public async Task CreateHandlingTask_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateHandlingTaskTool.ExecuteAsync(
            _readOnly, _task,
            warehouseId: WarehouseId, type: "Receiving", priority: "Normal");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateHandlingTask_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await CreateHandlingTaskTool.ExecuteAsync(
            _auth, _task,
            warehouseId: "", type: "Receiving", priority: "Normal");

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    // ---- AssignHandlingTask (write) ----

    [Fact]
    public async Task AssignHandlingTask_MissingAssignee_ReturnsErrorAsync()
    {
        var result = await AssignHandlingTaskTool.ExecuteAsync(
            _auth, _task, id: TaskId, assigneeUserId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("assigneeUserId is required");
    }

    // ---- StartHandlingTask (write) ----

    [Fact]
    public async Task StartHandlingTask_MissingId_ReturnsErrorAsync()
    {
        var result = await StartHandlingTaskTool.ExecuteAsync(_auth, _task, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- CompleteHandlingTask (write) ----

    [Fact]
    public async Task CompleteHandlingTask_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CompleteHandlingTaskTool.ExecuteAsync(_readOnly, _task, id: TaskId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ---- CancelHandlingTask (write) ----

    [Fact]
    public async Task CancelHandlingTask_MissingId_ReturnsErrorAsync()
    {
        var result = await CancelHandlingTaskTool.ExecuteAsync(_auth, _task, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GetIncomingShipment ----

    [Fact]
    public async Task GetIncomingShipment_MissingId_ReturnsErrorAsync()
    {
        var result = await GetIncomingShipmentTool.ExecuteAsync(_auth, _shipment, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetIncomingShipment_HappyPath_ReturnsShipmentAsync()
    {
        _shipment.GetIncomingShipmentAsync(
                Arg.Any<GetIncomingShipmentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new IncomingShipmentModel
            {
                Id = new GuidValue(ShipmentId),
                WarehouseId = new GuidValue(WarehouseId),
                Status = IncomingShipmentStatus.Pending,
                BoxCodeMode = BoxCodeMode.AtWarehouseReceive,
            }));

        var result = await GetIncomingShipmentTool.ExecuteAsync(_auth, _shipment, id: ShipmentId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(ShipmentId);
        json.GetProperty("status").GetString().Should().Be("Pending");
        json.GetProperty("boxCodeMode").GetString().Should().Be("AtWarehouseReceive");
    }

    // ---- ListIncomingShipments ----

    [Fact]
    public async Task ListIncomingShipments_HappyPath_ReturnsPaginatedAsync()
    {
        _shipment.ListIncomingShipmentsAsync(
                Arg.Any<ListIncomingShipmentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListIncomingShipmentsResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListIncomingShipmentsTool.ExecuteAsync(_auth, _shipment);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- ConfirmFactoryBoxArrival (write) ----

    [Fact]
    public async Task ConfirmFactoryBoxArrival_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ConfirmFactoryBoxArrivalTool.ExecuteAsync(
            _readOnly, _shipment, id: ShipmentId, arrivedBoxSerials: "[\"SN001\"]");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ConfirmFactoryBoxArrival_InvalidSerialsJson_ReturnsErrorAsync()
    {
        var result = await ConfirmFactoryBoxArrivalTool.ExecuteAsync(
            _auth, _shipment, id: ShipmentId, arrivedBoxSerials: "not-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("arrivedBoxSerials JSON is invalid");
    }

    // ---- LabelBoxAtWarehouse (write) ----

    [Fact]
    public async Task LabelBoxAtWarehouse_MissingBoxSerial_ReturnsErrorAsync()
    {
        var result = await LabelBoxAtWarehouseTool.ExecuteAsync(
            _auth, _shipment, id: ShipmentId, boxSerial: "", labelValue: "LBL-001");

        Parse(result).GetProperty("error").GetString().Should().Contain("boxSerial is required");
    }

    // ---- ConfirmBoxLabelled (write) ----

    [Fact]
    public async Task ConfirmBoxLabelled_MissingId_ReturnsErrorAsync()
    {
        var result = await ConfirmBoxLabelledTool.ExecuteAsync(
            _auth, _shipment, id: "", boxSerial: "SN001");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GetTransferOrder ----

    [Fact]
    public async Task GetTransferOrder_MissingId_ReturnsErrorAsync()
    {
        var result = await GetTransferOrderTool.ExecuteAsync(_auth, _transfer, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetTransferOrder_HappyPath_ReturnsOrderAsync()
    {
        _transfer.GetTransferOrderAsync(
                Arg.Any<GetTransferOrderRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new TransferOrderModel
            {
                Id = new GuidValue(OrderId),
                OriginStoreId = new GuidValue(WarehouseId),
                DestinationStoreId = new GuidValue(ZoneId),
                Status = TransferOrderStatus.Draft,
            }));

        var result = await GetTransferOrderTool.ExecuteAsync(_auth, _transfer, id: OrderId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(OrderId);
        json.GetProperty("status").GetString().Should().Be("Draft");
    }

    // ---- ListTransferOrders ----

    [Fact]
    public async Task ListTransferOrders_HappyPath_ReturnsPaginatedAsync()
    {
        _transfer.ListTransferOrdersAsync(
                Arg.Any<ListTransferOrdersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListTransferOrdersResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListTransferOrdersTool.ExecuteAsync(_auth, _transfer);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- CreateTransferOrder (write) ----

    [Fact]
    public async Task CreateTransferOrder_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateTransferOrderTool.ExecuteAsync(
            _readOnly, _transfer,
            originStoreId: WarehouseId,
            destinationStoreId: ZoneId,
            lineItems: /*lang=json,strict*/ "[{\"sku\":\"SKU001\",\"quantity\":10}]");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateTransferOrder_InvalidLineItemsJson_ReturnsErrorAsync()
    {
        var result = await CreateTransferOrderTool.ExecuteAsync(
            _auth, _transfer,
            originStoreId: WarehouseId,
            destinationStoreId: ZoneId,
            lineItems: "not-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("lineItems JSON is invalid");
    }

    // ---- MarkTransferOrderInTransit (write) ----

    [Fact]
    public async Task MarkTransferOrderInTransit_MissingId_ReturnsErrorAsync()
    {
        var result = await MarkTransferOrderInTransitTool.ExecuteAsync(_auth, _transfer, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- ReceiveTransferOrder (write) ----

    [Fact]
    public async Task ReceiveTransferOrder_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ReceiveTransferOrderTool.ExecuteAsync(_readOnly, _transfer, id: OrderId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ---- CancelTransferOrder (write) ----

    [Fact]
    public async Task CancelTransferOrder_MissingId_ReturnsErrorAsync()
    {
        var result = await CancelTransferOrderTool.ExecuteAsync(_auth, _transfer, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- GrpcUnavailable propagates ----

    [Fact]
    public async Task ListWarehouses_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _warehouse.ListWarehousesAsync(
                Arg.Any<ListWarehousesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListWarehousesResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ListWarehousesTool.ExecuteAsync(_auth, _warehouse);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
