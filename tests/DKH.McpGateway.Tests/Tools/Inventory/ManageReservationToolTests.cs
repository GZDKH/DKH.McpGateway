using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v1;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v1;
using DKH.McpGateway.Application.Tools.Inventory;
using DKH.Platform.Grpc.Common.Types;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public class ManageReservationToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly StockReservationService.StockReservationServiceClient _client =
        Substitute.For<StockReservationService.StockReservationServiceClient>();

    private static readonly string ProductId = Guid.NewGuid().ToString();
    private static readonly string WarehouseId = Guid.NewGuid().ToString();
    private static readonly string ReservationId = Guid.NewGuid().ToString();
    private static readonly string CartId = "cart-123";

    [Fact]
    public async Task Reserve_HappyPath_ReturnsReservationAsync()
    {
        SetupReserveStock(new StockReservationModel
        {
            ReservationId = new GuidValue(ReservationId),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            Quantity = 5,
            Status = ReservationStatusModel.Active,
            CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            ExpiresAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMinutes(30)),
        });

        var result = await ExecuteToolAsync("reserve",
            productId: ProductId, warehouseId: WarehouseId, quantity: 5);

        result.Should().Contain(ReservationId);
    }

    [Fact]
    public async Task Reserve_WithOptionalFields_SetsCartIdAndTtlAsync()
    {
        SetupReserveStock(new StockReservationModel
        {
            ReservationId = new GuidValue(ReservationId),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            Quantity = 3,
            CartId = CartId,
        });

        await ExecuteToolAsync("reserve",
            productId: ProductId, warehouseId: WarehouseId, quantity: 3,
            cartId: CartId, ttlMinutes: 60);

        _ = _client.Received(1).ReserveStockAsync(
            Arg.Is<ReserveStockRequest>(r =>
                r.CartId == CartId &&
                r.TtlMinutes == 60 &&
                r.Quantity == 3),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserve_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("reserve",
            warehouseId: WarehouseId, quantity: 5);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task Reserve_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("reserve",
            productId: ProductId, quantity: 5);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task Reserve_InvalidQuantity_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("reserve",
            productId: ProductId, warehouseId: WarehouseId, quantity: 0);

        Parse(result).GetProperty("error").GetString().Should().Contain("quantity must be a positive integer");
    }

    [Fact]
    public async Task Reserve_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageReservationTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "reserve",
            productId: ProductId, warehouseId: WarehouseId, quantity: 5);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Release_HappyPath_ReturnsOkAsync()
    {
        SetupReleaseReservation();

        var result = await ExecuteToolAsync("release", reservationId: ReservationId);

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Release_MissingReservationId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("release");

        Parse(result).GetProperty("error").GetString().Should().Contain("reservationId is required");
    }

    [Fact]
    public async Task Release_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageReservationTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "release",
            reservationId: ReservationId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Confirm_HappyPath_ReturnsOkAsync()
    {
        SetupConfirmReservation();

        var result = await ExecuteToolAsync("confirm", reservationId: ReservationId);

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Confirm_MissingReservationId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("confirm");

        Parse(result).GetProperty("error").GetString().Should().Contain("reservationId is required");
    }

    [Fact]
    public async Task Confirm_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageReservationTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "confirm",
            reservationId: ReservationId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Get_HappyPath_ReturnsReservationAsync()
    {
        SetupGetReservation(new StockReservationModel
        {
            ReservationId = new GuidValue(ReservationId),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            Quantity = 5,
            Status = ReservationStatusModel.Active,
            CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            ExpiresAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMinutes(30)),
        });

        var result = await ExecuteToolAsync("get", reservationId: ReservationId);

        result.Should().Contain(ReservationId);
    }

    [Fact]
    public async Task Get_MissingReservationId_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("get");

        Parse(result).GetProperty("error").GetString().Should().Contain("reservationId is required");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsPagedResultAsync()
    {
        SetupListReservations(new ListReservationsResponse
        {
            Items =
            {
                new StockReservationModel
                {
                    ReservationId = new GuidValue(ReservationId),
                    ProductId = new GuidValue(ProductId),
                    WarehouseId = new GuidValue(WarehouseId),
                    Quantity = 5,
                    Status = ReservationStatusModel.Active,
                    CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                    ExpiresAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMinutes(30)),
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
        SetupListReservations(new ListReservationsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 2, PageSize = 10 },
        });

        await ExecuteToolAsync("list", cartId: CartId, status: "active", page: 2, pageSize: 10);

        _ = _client.Received(1).ListReservationsAsync(
            Arg.Is<ListReservationsRequest>(r =>
                r.CartId == CartId &&
                r.Status == ReservationStatusModel.Active &&
                r.Pagination.Page == 2 &&
                r.Pagination.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync("invalid");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Theory]
    [InlineData("RESERVE")]
    [InlineData("Reserve")]
    [InlineData("RELEASE")]
    [InlineData("LIST")]
    public async Task Action_IsCaseInsensitiveAsync(string action)
    {
        SetupReserveStock(new StockReservationModel
        {
            ReservationId = new GuidValue(ReservationId),
            ProductId = new GuidValue(ProductId),
            WarehouseId = new GuidValue(WarehouseId),
            Quantity = 1,
            CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            ExpiresAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMinutes(30)),
        });
        SetupReleaseReservation();
        SetupListReservations(new ListReservationsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
        });

        var result = await ExecuteToolAsync(action,
            productId: ProductId, warehouseId: WarehouseId, quantity: 1,
            reservationId: ReservationId);

        result.Should().NotContain("Unknown action");
    }

    [Fact]
    public async Task GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ReserveStockAsync(
                Arg.Any<ReserveStockRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<StockReservationModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync("reserve",
            productId: ProductId, warehouseId: WarehouseId, quantity: 5);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string action,
        string? reservationId = null,
        string? productId = null,
        string? variantId = null,
        string? warehouseId = null,
        int? quantity = null,
        string? cartId = null,
        int? ttlMinutes = null,
        string? status = null,
        int? page = null,
        int? pageSize = null)
        => ManageReservationTool.ExecuteAsync(
            _auth, _client,
            action: action, reservationId: reservationId,
            productId: productId, variantId: variantId, warehouseId: warehouseId,
            quantity: quantity, cartId: cartId, ttlMinutes: ttlMinutes,
            status: status, page: page, pageSize: pageSize);

    private void SetupReserveStock(StockReservationModel response)
        => _client.ReserveStockAsync(
                Arg.Any<ReserveStockRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupReleaseReservation()
        => _client.ReleaseReservationAsync(
                Arg.Any<ReleaseReservationRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private void SetupConfirmReservation()
        => _client.ConfirmReservationAsync(
                Arg.Any<ConfirmReservationRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

    private void SetupGetReservation(StockReservationModel response)
        => _client.GetReservationAsync(
                Arg.Any<GetReservationRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupListReservations(ListReservationsResponse response)
        => _client.ListReservationsAsync(
                Arg.Any<ListReservationsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
