using DKH.DeliveryService.Contracts.Delivery.Api.Allocation.v1;
using DKH.DeliveryService.Contracts.Delivery.Api.Analytics.v1;
using DKH.DeliveryService.Contracts.Delivery.Api.Cancellation.v1;
using DKH.DeliveryService.Contracts.Delivery.Api.Claims.v1;
using DKH.DeliveryService.Contracts.Delivery.Api.CustomsLink.v1;
using DKH.DeliveryService.Contracts.Delivery.Api.DeliveryCrud.v1;
using DKH.DeliveryService.Contracts.Delivery.Api.Dispatch.v1;
using DKH.DeliveryService.Contracts.Delivery.Api.Settlements.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;
using DKH.McpGateway.Application.Tools.Delivery;
using DKH.McpGateway.Application.Tools.DeliveryClaims;
using DKH.McpGateway.Application.Tools.DeliverySettlements;
using DKH.Platform.Grpc.Common.Types;

namespace DKH.McpGateway.Tests.Tools.Delivery;

public class DeliveryToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly IApiKeyContext _readOnly = ApiKeyContextMocks.ReadOnly();

    private readonly DeliveryCrudService.DeliveryCrudServiceClient _crud =
        Substitute.For<DeliveryCrudService.DeliveryCrudServiceClient>();

    private readonly DispatchService.DispatchServiceClient _dispatch =
        Substitute.For<DispatchService.DispatchServiceClient>();

    private readonly ClaimsService.ClaimsServiceClient _claims =
        Substitute.For<ClaimsService.ClaimsServiceClient>();

    private readonly SettlementsService.SettlementsServiceClient _settlements =
        Substitute.For<SettlementsService.SettlementsServiceClient>();

    private readonly AnalyticsService.AnalyticsServiceClient _analytics =
        Substitute.For<AnalyticsService.AnalyticsServiceClient>();

    private readonly AllocationService.AllocationServiceClient _allocation =
        Substitute.For<AllocationService.AllocationServiceClient>();

    private readonly CancellationService.CancellationServiceClient _cancellation =
        Substitute.For<CancellationService.CancellationServiceClient>();

    private readonly CustomsLinkService.CustomsLinkServiceClient _customsLink =
        Substitute.For<CustomsLinkService.CustomsLinkServiceClient>();

    private static readonly string FulfillmentId = Guid.NewGuid().ToString();
    private static readonly string ShipmentId = Guid.NewGuid().ToString();
    private static readonly string QuoteId = Guid.NewGuid().ToString();
    private static readonly string ClaimId = Guid.NewGuid().ToString();
    private static readonly string LegId = Guid.NewGuid().ToString();
    private static readonly string CarrierId = Guid.NewGuid().ToString();
    private static readonly string PartnerId = Guid.NewGuid().ToString();
    private static readonly string DeclarationId = Guid.NewGuid().ToString();

    // ---- GetDeliveryQuote ----

    [Fact]
    public async Task GetDeliveryQuote_MissingId_ReturnsErrorAsync()
    {
        var result = await GetDeliveryQuoteTool.ExecuteAsync(_auth, _crud, quoteId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("quoteId is required");
    }

    [Fact]
    public async Task GetDeliveryQuote_HappyPath_ReturnsQuoteAsync()
    {
        _crud.GetQuoteAsync(
                Arg.Any<GetQuoteRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new QuoteModel
            {
                Id = new GuidValue(QuoteId),
                Status = QuoteStatus.Accepted,
            }));

        var result = await GetDeliveryQuoteTool.ExecuteAsync(_auth, _crud, quoteId: QuoteId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(QuoteId);
        json.GetProperty("status").GetString().Should().Be("Accepted");
    }

    // ---- CreateDeliveryQuote (write) ----

    [Fact]
    public async Task CreateDeliveryQuote_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CreateDeliveryQuoteTool.ExecuteAsync(
            _readOnly, _crud,
            carrierCounterpartyId: CarrierId,
            serviceLevel: "Standard",
            amount: "100.00",
            currency: "USD",
            expiresAt: "2026-12-31T00:00:00Z");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateDeliveryQuote_MissingCarrier_ReturnsErrorAsync()
    {
        var result = await CreateDeliveryQuoteTool.ExecuteAsync(
            _auth, _crud,
            carrierCounterpartyId: "",
            serviceLevel: "Standard",
            amount: "100.00",
            currency: "USD",
            expiresAt: "2026-12-31T00:00:00Z");

        Parse(result).GetProperty("error").GetString().Should().Contain("carrierCounterpartyId is required");
    }

    // ---- GetFulfillment ----

    [Fact]
    public async Task GetFulfillment_MissingId_ReturnsErrorAsync()
    {
        var result = await GetFulfillmentTool.ExecuteAsync(_auth, _crud, fulfillmentId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("fulfillmentId is required");
    }

    [Fact]
    public async Task GetFulfillment_PiiOmitted_NoDeliveryAddressAsync()
    {
        _crud.GetFulfillmentAsync(
                Arg.Any<GetFulfillmentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new FulfillmentModel
            {
                Id = new GuidValue(FulfillmentId),
                Status = FulfillmentStatus.Dispatched,
                DeliveryAddress = new FulfillmentAddress
                {
                    RecipientName = "John Doe",
                    RecipientPhone = "+1555000000",
                    City = "New York",
                },
            }));

        var result = await GetFulfillmentTool.ExecuteAsync(_auth, _crud, fulfillmentId: FulfillmentId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(FulfillmentId);
        json.GetProperty("status").GetString().Should().Be("Dispatched");
        json.TryGetProperty("deliveryAddress", out _).Should().BeFalse();
        json.TryGetProperty("recipientName", out _).Should().BeFalse();
    }

    // ---- GetShipment ----

    [Fact]
    public async Task GetShipment_MissingId_ReturnsErrorAsync()
    {
        var result = await GetShipmentTool.ExecuteAsync(_auth, _crud, shipmentId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("shipmentId is required");
    }

    [Fact]
    public async Task GetShipment_HappyPath_ReturnsShipmentAsync()
    {
        _crud.GetShipmentAsync(
                Arg.Any<GetShipmentRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ShipmentModel
            {
                Id = new GuidValue(ShipmentId),
                Status = ShipmentStatus.InTransit,
                TrackingNumber = "TRK123",
            }));

        var result = await GetShipmentTool.ExecuteAsync(_auth, _crud, shipmentId: ShipmentId);

        var json = Parse(result);
        json.GetProperty("id").GetString().Should().Be(ShipmentId);
        json.GetProperty("status").GetString().Should().Be("InTransit");
        json.GetProperty("trackingNumber").GetString().Should().Be("TRK123");
    }

    // ---- ListShipments ----

    [Fact]
    public async Task ListShipments_HappyPath_ReturnsMetadataAsync()
    {
        _crud.ListShipmentsAsync(
                Arg.Any<ListShipmentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListShipmentsResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ListShipmentsTool.ExecuteAsync(_auth, _crud);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListShipments_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _crud.ListShipmentsAsync(
                Arg.Any<ListShipmentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListShipmentsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ListShipmentsTool.ExecuteAsync(_auth, _crud);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    // ---- PackHandlingUnits (write) ----

    [Fact]
    public async Task PackHandlingUnits_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => PackHandlingUnitsTool.ExecuteAsync(
            _readOnly, _dispatch, fulfillmentId: FulfillmentId, units: "[]");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task PackHandlingUnits_MissingFulfillmentId_ReturnsErrorAsync()
    {
        var result = await PackHandlingUnitsTool.ExecuteAsync(_auth, _dispatch, fulfillmentId: "", units: "[]");

        Parse(result).GetProperty("error").GetString().Should().Contain("fulfillmentId is required");
    }

    [Fact]
    public async Task PackHandlingUnits_InvalidJson_ReturnsErrorAsync()
    {
        var result = await PackHandlingUnitsTool.ExecuteAsync(
            _auth, _dispatch, fulfillmentId: FulfillmentId, units: "not-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("units JSON is invalid");
    }

    // ---- BookLeg (write) ----

    [Fact]
    public async Task BookLeg_MissingLegId_ReturnsErrorAsync()
    {
        var result = await BookLegTool.ExecuteAsync(
            _auth, _dispatch, legId: "", carrierId: CarrierId, confirmationNumber: "CNF001");

        Parse(result).GetProperty("error").GetString().Should().Contain("legId is required");
    }

    [Fact]
    public async Task BookLeg_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => BookLegTool.ExecuteAsync(
            _readOnly, _dispatch, legId: LegId, carrierId: CarrierId, confirmationNumber: "CNF001");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ---- ConfirmDispatch (write) ----

    [Fact]
    public async Task ConfirmDispatch_MissingDocumentRefs_ReturnsErrorAsync()
    {
        var result = await ConfirmDispatchTool.ExecuteAsync(
            _auth, _dispatch, fulfillmentId: FulfillmentId, legId: LegId, documentRefs: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("documentRefs is required");
    }

    // ---- OpenDeliveryClaim (write) ----

    [Fact]
    public async Task OpenDeliveryClaim_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => OpenDeliveryClaimTool.ExecuteAsync(
            _readOnly, _claims,
            fulfillmentId: FulfillmentId,
            shipmentId: ShipmentId,
            type: "Damage",
            amountClaimed: 100.0,
            currency: "USD",
            description: "Box damaged");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task OpenDeliveryClaim_MissingType_ReturnsErrorAsync()
    {
        var result = await OpenDeliveryClaimTool.ExecuteAsync(
            _auth, _claims,
            fulfillmentId: FulfillmentId,
            shipmentId: ShipmentId,
            type: "",
            amountClaimed: 100.0,
            currency: "USD",
            description: "Box damaged");

        Parse(result).GetProperty("error").GetString().Should().Contain("type is required");
    }

    // ---- AddClaimEvidence (write) ----

    [Fact]
    public async Task AddClaimEvidence_MissingClaimId_ReturnsErrorAsync()
    {
        var result = await AddClaimEvidenceTool.ExecuteAsync(
            _auth, _claims, claimId: "", documentRef: "doc-001", description: "Photo");

        Parse(result).GetProperty("error").GetString().Should().Contain("claimId is required");
    }

    // ---- UpdateClaimStatus (write) ----

    [Fact]
    public async Task UpdateClaimStatus_MissingNextStatus_ReturnsErrorAsync()
    {
        var result = await UpdateClaimStatusTool.ExecuteAsync(
            _auth, _claims, claimId: ClaimId, nextStatus: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("nextStatus is required");
    }

    // ---- ListDeliveryClaims ----

    [Fact]
    public async Task ListDeliveryClaims_HappyPath_ReturnsItemsAsync()
    {
        _claims.ListClaimsAsync(
                Arg.Any<ListClaimsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListClaimsResponse()));

        var result = await ListDeliveryClaimsTool.ExecuteAsync(_auth, _claims);

        Parse(result).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- GetSettlement ----

    [Fact]
    public async Task GetSettlement_MissingFulfillmentId_ReturnsErrorAsync()
    {
        var result = await GetSettlementTool.ExecuteAsync(_auth, _settlements, fulfillmentId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("fulfillmentId is required");
    }

    // ---- ListSettlements ----

    [Fact]
    public async Task ListSettlements_HappyPath_ReturnsItemsAsync()
    {
        _settlements.ListSettlementsAsync(
                Arg.Any<ListSettlementsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListSettlementsResponse()));

        var result = await ListSettlementsTool.ExecuteAsync(_auth, _settlements);

        Parse(result).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- ImportCarrierInvoice (write) ----

    [Fact]
    public async Task ImportCarrierInvoice_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ImportCarrierInvoiceTool.ExecuteAsync(
            _readOnly, _settlements,
            fulfillmentId: FulfillmentId,
            shipmentId: ShipmentId,
            carrierPartyId: CarrierId,
            currency: "USD",
            externalRef: "INV-001",
            lines: "[]");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ImportCarrierInvoice_InvalidJson_ReturnsErrorAsync()
    {
        var result = await ImportCarrierInvoiceTool.ExecuteAsync(
            _auth, _settlements,
            fulfillmentId: FulfillmentId,
            shipmentId: ShipmentId,
            carrierPartyId: CarrierId,
            currency: "USD",
            externalRef: "INV-001",
            lines: "bad-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("lines JSON is invalid");
    }

    // ---- GetSlaAnalytics ----

    [Fact]
    public async Task GetSlaAnalytics_MissingPartnerId_ReturnsErrorAsync()
    {
        var result = await GetSlaAnalyticsTool.ExecuteAsync(_auth, _analytics, partnerId: "", period: "Month");

        Parse(result).GetProperty("error").GetString().Should().Contain("partnerId is required");
    }

    [Fact]
    public async Task GetSlaAnalytics_HappyPath_ReturnsItemsAsync()
    {
        _analytics.GetSlaAnalyticsAsync(
                Arg.Any<GetSlaAnalyticsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetSlaAnalyticsResponse()));

        var result = await GetSlaAnalyticsTool.ExecuteAsync(_auth, _analytics, partnerId: PartnerId, period: "Month");

        Parse(result).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ---- AllocateFulfillment (write) ----

    [Fact]
    public async Task AllocateFulfillment_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => AllocateFulfillmentTool.ExecuteAsync(_readOnly, _allocation, fulfillmentId: FulfillmentId, lines: "[]");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AllocateFulfillment_InvalidLinesJson_ReturnsErrorAsync()
    {
        var result = await AllocateFulfillmentTool.ExecuteAsync(
            _auth, _allocation, fulfillmentId: FulfillmentId, lines: "not-json");

        Parse(result).GetProperty("error").GetString().Should().Contain("lines JSON is invalid");
    }

    // ---- AutoAllocateFulfillment (write) ----

    [Fact]
    public async Task AutoAllocateFulfillment_MissingFulfillmentId_ReturnsErrorAsync()
    {
        var result = await AutoAllocateFulfillmentTool.ExecuteAsync(
            _auth, _allocation, fulfillmentId: "", demands: "[]");

        Parse(result).GetProperty("error").GetString().Should().Contain("fulfillmentId is required");
    }

    // ---- CancelFulfillment (write) ----

    [Fact]
    public async Task CancelFulfillment_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CancelFulfillmentTool.ExecuteAsync(
            _readOnly, _cancellation, fulfillmentId: FulfillmentId, reason: "CustomerRequest");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CancelFulfillment_MissingReason_ReturnsErrorAsync()
    {
        var result = await CancelFulfillmentTool.ExecuteAsync(
            _auth, _cancellation, fulfillmentId: FulfillmentId, reason: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("reason is required");
    }

    // ---- AttachCustomsDeclaration (write) ----

    [Fact]
    public async Task AttachCustomsDeclaration_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => AttachCustomsDeclarationTool.ExecuteAsync(
            _readOnly, _customsLink, fulfillmentId: FulfillmentId, declarationId: DeclarationId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task AttachCustomsDeclaration_MissingDeclarationId_ReturnsErrorAsync()
    {
        var result = await AttachCustomsDeclarationTool.ExecuteAsync(
            _auth, _customsLink, fulfillmentId: FulfillmentId, declarationId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("declarationId is required");
    }

    // ---- GetCustomsDeclarations ----

    [Fact]
    public async Task GetCustomsDeclarations_MissingFulfillmentId_ReturnsErrorAsync()
    {
        var result = await GetCustomsDeclarationsTool.ExecuteAsync(
            _auth, _customsLink, fulfillmentId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("fulfillmentId is required");
    }

    [Fact]
    public async Task GetCustomsDeclarations_HappyPath_ReturnsDeclarationIdsAsync()
    {
        var response = new GetDeclarationsResponse();
        response.DeclarationIds.Add(new GuidValue(DeclarationId));

        _customsLink.GetDeclarationsAsync(
                Arg.Any<GetDeclarationsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var result = await GetCustomsDeclarationsTool.ExecuteAsync(
            _auth, _customsLink, fulfillmentId: FulfillmentId);

        var json = Parse(result);
        json.GetProperty("declarationIds").GetArrayLength().Should().Be(1);
        json.GetProperty("declarationIds")[0].GetString().Should().Be(DeclarationId);
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
