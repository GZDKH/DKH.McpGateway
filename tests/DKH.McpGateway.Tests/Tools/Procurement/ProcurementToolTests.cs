using DKH.McpGateway.Application.Tools.Procurement;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProcurementService.Contracts.Models.V1;
using DKH.ProcurementService.Contracts.Services.V1;
using ProcurementGrpc = DKH.ProcurementService.Contracts.Services.V1.Procurement;

namespace DKH.McpGateway.Tests.Tools.Procurement;

public class ProcurementToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly IApiKeyContext _readOnly = ApiKeyContextMocks.ReadOnly();

    private readonly ProcurementGrpc.ProcurementClient _client =
        Substitute.For<ProcurementGrpc.ProcurementClient>();

    private static readonly string PoId = Guid.NewGuid().ToString();
    private static readonly string InspectionId = Guid.NewGuid().ToString();
    private static readonly string InspectorId = Guid.NewGuid().ToString();
    private static readonly string StorefrontId = Guid.NewGuid().ToString();
    private static readonly string ReturnId = Guid.NewGuid().ToString();
    private static readonly string OfferId = Guid.NewGuid().ToString();
    private static readonly string CatalogRef = Guid.NewGuid().ToString();
    private static readonly string SellerId = Guid.NewGuid().ToString();
    private static readonly string OrderId = Guid.NewGuid().ToString();

    // ---- GetPurchaseOrder ----

    [Fact]
    public async Task GetPurchaseOrder_MissingId_ReturnsErrorAsync()
    {
        var result = await GetPurchaseOrderTool.ExecuteAsync(_auth, _client, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetPurchaseOrder_HappyPath_ReturnsMappedPoAsync()
    {
        _client.GetPurchaseOrderAsync(
                Arg.Any<GetPurchaseOrderRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PurchaseOrderModel
            {
                Id = new GuidValue(PoId),
                Status = PurchaseOrderStatus.InProgress,
                Currency = "USD",
                RequiresInspection = true,
            }));

        var result = await GetPurchaseOrderTool.ExecuteAsync(_auth, _client, id: PoId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(PoId);
        json.GetProperty("status").GetString().Should().Be("InProgress");
        json.GetProperty("currency").GetString().Should().Be("USD");
        json.GetProperty("requiresInspection").GetBoolean().Should().BeTrue();
    }

    // ---- ListOpenProductionOrders ----

    [Fact]
    public async Task ListOpenProductionOrders_HappyPath_ReturnsPaginatedAsync()
    {
        _client.ListOpenProductionOrdersAsync(
                Arg.Any<ListOpenProductionOrdersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListPurchaseOrdersResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListOpenProductionOrdersTool.ExecuteAsync(_auth, _client);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- GetInspection ----

    [Fact]
    public async Task GetInspection_MissingId_ReturnsErrorAsync()
    {
        var result = await GetInspectionTool.ExecuteAsync(_auth, _client, inspectionId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("inspectionId is required");
    }

    [Fact]
    public async Task GetInspection_HappyPath_ReturnsMappedInspectionAsync()
    {
        _client.GetInspectionAsync(
                Arg.Any<GetInspectionRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new InspectionModel
            {
                Id = new GuidValue(InspectionId),
                PurchaseOrderId = new GuidValue(PoId),
                Status = InspectionStatus.Scheduled,
                SampleSize = 10,
                Decision = InspectionDecision.Unspecified,
            }));

        var result = await GetInspectionTool.ExecuteAsync(_auth, _client, inspectionId: InspectionId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(InspectionId);
        json.GetProperty("status").GetString().Should().Be("Scheduled");
        json.GetProperty("sampleSize").GetInt32().Should().Be(10);
    }

    // ---- ListInspections ----

    [Fact]
    public async Task ListInspections_HappyPath_ReturnsPaginatedAsync()
    {
        _client.ListInspectionsAsync(
                Arg.Any<ListInspectionsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListInspectionsResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListInspectionsTool.ExecuteAsync(_auth, _client);

        Parse(result).GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // ---- ListAssignedInspections ----

    [Fact]
    public async Task ListAssignedInspections_MissingInspectorId_ReturnsErrorAsync()
    {
        var result = await ListAssignedInspectionsTool.ExecuteAsync(_auth, _client, inspectorId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("inspectorId is required");
    }

    // ---- ScheduleInspection (write) ----

    [Fact]
    public async Task ScheduleInspection_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ScheduleInspectionTool.ExecuteAsync(
            _readOnly, _client,
            purchaseOrderId: PoId, inspectorId: InspectorId,
            scheduledAt: "2026-07-01T10:00:00Z", sampleSize: 5, inspectionType: "PreShipment");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ScheduleInspection_MissingPoId_ReturnsErrorAsync()
    {
        var result = await ScheduleInspectionTool.ExecuteAsync(
            _auth, _client,
            purchaseOrderId: "", inspectorId: InspectorId,
            scheduledAt: "2026-07-01T10:00:00Z", sampleSize: 5, inspectionType: "PreShipment");

        Parse(result).GetProperty("error").GetString().Should().Contain("purchaseOrderId is required");
    }

    // ---- StartInspection (write) ----

    [Fact]
    public async Task StartInspection_MissingId_ReturnsErrorAsync()
    {
        var result = await StartInspectionTool.ExecuteAsync(_auth, _client, inspectionId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("inspectionId is required");
    }

    // ---- CancelInspection (write) ----

    [Fact]
    public async Task CancelInspection_MissingId_ReturnsErrorAsync()
    {
        var result = await CancelInspectionTool.ExecuteAsync(_auth, _client, inspectionId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("inspectionId is required");
    }

    // ---- CompleteInspection (write) ----

    [Fact]
    public async Task CompleteInspection_MissingDecision_ReturnsErrorAsync()
    {
        var result = await CompleteInspectionTool.ExecuteAsync(
            _auth, _client, inspectionId: InspectionId, decision: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("decision is required");
    }

    // ---- GenerateInspectionReport (write) ----

    [Fact]
    public async Task GenerateInspectionReport_MissingId_ReturnsErrorAsync()
    {
        var result = await GenerateInspectionReportTool.ExecuteAsync(_auth, _client, inspectionId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("inspectionId is required");
    }

    [Fact]
    public async Task GenerateInspectionReport_OmitsContentBytes_ReturnsMetadataOnlyAsync()
    {
        _client.GenerateInspectionReportAsync(
                Arg.Any<GenerateInspectionReportRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new InspectionReportFileModel
            {
                FileName = "report.pdf",
                ContentType = "application/pdf",
                Content = Google.Protobuf.ByteString.CopyFrom(new byte[1024]),
            }));

        var result = await GenerateInspectionReportTool.ExecuteAsync(
            _auth, _client, inspectionId: InspectionId);

        var json = Parse(result);
        json.GetProperty("fileName").GetString().Should().Be("report.pdf");
        json.GetProperty("contentType").GetString().Should().Be("application/pdf");
        json.GetProperty("contentLength").GetInt32().Should().Be(1024);
        // PII mask: content bytes must NOT be present
        json.TryGetProperty("content", out _).Should().BeFalse();
    }

    // ---- ListLowStockToProduce ----

    [Fact]
    public async Task ListLowStockToProduce_HappyPath_ReturnsPaginatedAsync()
    {
        _client.ListLowStockToProduceAsync(
                Arg.Any<ListLowStockToProduceRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListLowStockToProduceResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListLowStockToProduceTool.ExecuteAsync(_auth, _client);

        Parse(result).GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // ---- GetProductDemand ----

    [Fact]
    public async Task GetProductDemand_MissingStorefrontId_ReturnsErrorAsync()
    {
        var result = await GetProductDemandTool.ExecuteAsync(_auth, _client, storefrontId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontId is required");
    }

    [Fact]
    public async Task GetProductDemand_OmitsCustomerId_ReturnsOnlyAggregateFieldsAsync()
    {
        var requestId = Guid.NewGuid().ToString();
        _client.GetProductDemandAsync(
                Arg.Any<GetProductDemandRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetProductDemandResponse
            {
                Items =
                {
                    new DemandSignalEntryModel
                    {
                        ProductRequestId = new GuidValue(requestId),
                        CustomerId = "pii-customer-id",
                    },
                },
            }));

        var result = await GetProductDemandTool.ExecuteAsync(_auth, _client, storefrontId: StorefrontId);

        var json = Parse(result);
        var item = json.GetProperty("items").EnumerateArray().First();
        item.GetProperty("productRequestId").GetString().Should().Be(requestId);
        // PII mask: customerId must NOT appear in any item
        item.TryGetProperty("customerId", out _).Should().BeFalse();
    }

    // ---- GetReceivingAct ----

    [Fact]
    public async Task GetReceivingAct_MissingId_ReturnsErrorAsync()
    {
        var result = await GetReceivingActTool.ExecuteAsync(_auth, _client, receivingActId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("receivingActId is required");
    }

    // ---- SignReceivingAct (write) ----

    [Fact]
    public async Task SignReceivingAct_MissingPoId_ReturnsErrorAsync()
    {
        var result = await SignReceivingActTool.ExecuteAsync(
            _auth, _client, purchaseOrderId: "", signedBy: InspectorId, warehouseId: ReturnId);

        Parse(result).GetProperty("error").GetString().Should().Contain("purchaseOrderId is required");
    }

    [Fact]
    public async Task SignReceivingAct_InvalidLinesJson_ReturnsErrorAsync()
    {
        var result = await SignReceivingActTool.ExecuteAsync(
            _auth, _client,
            purchaseOrderId: PoId, signedBy: InspectorId, warehouseId: ReturnId,
            acceptedLines: "not-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("acceptedLines JSON is invalid");
    }

    // ---- GetSupplierReturnOrder ----

    [Fact]
    public async Task GetSupplierReturnOrder_MissingId_ReturnsErrorAsync()
    {
        var result = await GetSupplierReturnOrderTool.ExecuteAsync(_auth, _client, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    // ---- CreateSupplierReturnOrder (write) ----

    [Fact]
    public async Task CreateSupplierReturnOrder_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateSupplierReturnOrderTool.ExecuteAsync(
            _readOnly, _client,
            purchaseOrderId: PoId, supplierCounterpartyId: SellerId,
            sourceWarehouseId: ReturnId, createdBy: InspectorId,
            lines: "[{\"catalogRef\":\"" + CatalogRef + "\",\"quantity\":5,\"reason\":\"damaged\"}]");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateSupplierReturnOrder_InvalidLinesJson_ReturnsErrorAsync()
    {
        var result = await CreateSupplierReturnOrderTool.ExecuteAsync(
            _auth, _client,
            purchaseOrderId: PoId, supplierCounterpartyId: SellerId,
            sourceWarehouseId: ReturnId, createdBy: InspectorId,
            lines: "not-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("lines JSON is invalid");
    }

    // ---- GenerateSupplierReturnManifest (write) ----

    [Fact]
    public async Task GenerateSupplierReturnManifest_MissingManifestNumber_ReturnsErrorAsync()
    {
        var result = await GenerateSupplierReturnManifestTool.ExecuteAsync(
            _auth, _client, id: ReturnId, manifestNumber: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("manifestNumber is required");
    }

    // ---- GetSourcingOffer ----

    [Fact]
    public async Task GetSourcingOffer_MissingId_ReturnsErrorAsync()
    {
        var result = await GetSourcingOfferTool.ExecuteAsync(_auth, _client, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task GetSourcingOffer_HappyPath_ReturnsMappedOfferAsync()
    {
        _client.GetSourcingOfferAsync(
                Arg.Any<GetSourcingOfferRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SourcingOfferModel
            {
                Id = new GuidValue(OfferId),
                CatalogRef = new GuidValue(CatalogRef),
                SellerCounterpartyId = new GuidValue(SellerId),
                OriginCountry = "CN",
                Status = SourcingOfferStatus.Active,
            }));

        var result = await GetSourcingOfferTool.ExecuteAsync(_auth, _client, id: OfferId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(OfferId);
        json.GetProperty("originCountry").GetString().Should().Be("CN");
        json.GetProperty("status").GetString().Should().Be("Active");
    }

    // ---- ListSourcingOffersByProduct ----

    [Fact]
    public async Task ListSourcingOffersByProduct_MissingCatalogRef_ReturnsErrorAsync()
    {
        var result = await ListSourcingOffersByProductTool.ExecuteAsync(_auth, _client, catalogRef: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("catalogRef is required");
    }

    // ---- SetSourcingOfferStatus (write) ----

    [Fact]
    public async Task SetSourcingOfferStatus_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => SetSourcingOfferStatusTool.ExecuteAsync(
            _readOnly, _client, id: OfferId, action: "Activate");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SetSourcingOfferStatus_MissingAction_ReturnsErrorAsync()
    {
        var result = await SetSourcingOfferStatusTool.ExecuteAsync(_auth, _client, id: OfferId, action: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("action is required");
    }

    // ---- GetCustomCustomerOrder ----

    [Fact]
    public async Task GetCustomCustomerOrder_MissingId_ReturnsErrorAsync()
    {
        var result = await GetCustomCustomerOrderTool.ExecuteAsync(_auth, _client, orderId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("orderId is required");
    }

    [Fact]
    public async Task GetCustomCustomerOrder_OmitsCustomerIdAndDeliveryAddress_PiiMaskedAsync()
    {
        _client.GetCustomCustomerOrderAsync(
                Arg.Any<GetCustomCustomerOrderRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CustomCustomerOrderModel
            {
                CustomerId = new GuidValue(Guid.NewGuid().ToString()),
                DeliveryAddress = "123 Secret St, City",
                BaseProductCatalogRef = CatalogRef,
            }));

        var result = await GetCustomCustomerOrderTool.ExecuteAsync(_auth, _client, orderId: OrderId);

        var json = Parse(result);
        // PII mask: customerId and deliveryAddress must NOT appear
        json.TryGetProperty("customerId", out _).Should().BeFalse();
        json.TryGetProperty("deliveryAddress", out _).Should().BeFalse();
        json.GetProperty("baseProductCatalogRef").GetString().Should().Be(CatalogRef);
    }

    // ---- ApproveCustomCustomerOrder (write) ----

    [Fact]
    public async Task ApproveCustomCustomerOrder_MissingManagerId_ReturnsErrorAsync()
    {
        var result = await ApproveCustomCustomerOrderTool.ExecuteAsync(
            _auth, _client, orderId: OrderId, managerUserId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("managerUserId is required");
    }

    // ---- ConfirmCustomOrderWithDeposit (write) ----

    [Fact]
    public async Task ConfirmCustomOrderWithDeposit_MissingId_ReturnsErrorAsync()
    {
        var result = await ConfirmCustomOrderWithDepositTool.ExecuteAsync(_auth, _client, orderId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("orderId is required");
    }

    // ---- SendCustomOrderToFactory (write) ----

    [Fact]
    public async Task SendCustomOrderToFactory_MissingFactoryPoId_ReturnsErrorAsync()
    {
        var result = await SendCustomOrderToFactoryTool.ExecuteAsync(
            _auth, _client, orderId: OrderId, factoryProductionOrderId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("factoryProductionOrderId is required");
    }

    // ---- DeliverCustomOrderToCustomer (write) ----

    [Fact]
    public async Task DeliverCustomOrderToCustomer_MissingId_ReturnsErrorAsync()
    {
        var result = await DeliverCustomOrderToCustomerTool.ExecuteAsync(_auth, _client, orderId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("orderId is required");
    }

    // ---- gRPC unavailable ----

    [Fact]
    public async Task GetPurchaseOrder_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetPurchaseOrderAsync(
                Arg.Any<GetPurchaseOrderRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<PurchaseOrderModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => GetPurchaseOrderTool.ExecuteAsync(_auth, _client, id: PoId);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
