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

    [Fact]
    public async Task Reserve_HappyPath_ReturnsReservationAsync()
    {
        var productId = Guid.NewGuid().ToString();
        var warehouseId = Guid.NewGuid().ToString();
        var reservationId = Guid.NewGuid().ToString();

        _client.ReserveStockAsync(
                Arg.Any<ReserveStockRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new StockReservationModel
            {
                ReservationId = new GuidValue(reservationId),
                ProductId = new GuidValue(productId),
                WarehouseId = new GuidValue(warehouseId),
                Quantity = 5,
                Status = ReservationStatusModel.Active,
                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                ExpiresAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddMinutes(30)),
            }));

        var result = await ManageReservationTool.ExecuteAsync(
            _auth, _client, "reserve", productId: productId, warehouseId: warehouseId, quantity: 5);

        result.Should().Contain(reservationId);
    }

    [Fact]
    public async Task Reserve_MissingProductId_ReturnsErrorAsync()
    {
        var result = await ManageReservationTool.ExecuteAsync(
            _auth, _client, "reserve", warehouseId: Guid.NewGuid().ToString(), quantity: 5);

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task Reserve_MissingWarehouseId_ReturnsErrorAsync()
    {
        var result = await ManageReservationTool.ExecuteAsync(
            _auth, _client, "reserve", productId: Guid.NewGuid().ToString(), quantity: 5);

        Parse(result).GetProperty("error").GetString().Should().Contain("warehouseId is required");
    }

    [Fact]
    public async Task Reserve_InvalidQuantity_ReturnsErrorAsync()
    {
        var result = await ManageReservationTool.ExecuteAsync(
            _auth, _client, "reserve",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString(), quantity: 0);

        Parse(result).GetProperty("error").GetString().Should().Contain("quantity must be a positive integer");
    }

    [Fact]
    public async Task Release_HappyPath_ReturnsOkAsync()
    {
        var reservationId = Guid.NewGuid().ToString();
        _client.ReleaseReservationAsync(
                Arg.Any<ReleaseReservationRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

        var result = await ManageReservationTool.ExecuteAsync(_auth, _client, "release", reservationId: reservationId);

        Parse(result).GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Release_MissingReservationId_ReturnsErrorAsync()
    {
        var result = await ManageReservationTool.ExecuteAsync(_auth, _client, "release");

        Parse(result).GetProperty("error").GetString().Should().Contain("reservationId is required");
    }

    [Fact]
    public async Task Confirm_MissingReservationId_ReturnsErrorAsync()
    {
        var result = await ManageReservationTool.ExecuteAsync(_auth, _client, "confirm");

        Parse(result).GetProperty("error").GetString().Should().Contain("reservationId is required");
    }

    [Fact]
    public async Task Get_MissingReservationId_ReturnsErrorAsync()
    {
        var result = await ManageReservationTool.ExecuteAsync(_auth, _client, "get");

        Parse(result).GetProperty("error").GetString().Should().Contain("reservationId is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageReservationTool.ExecuteAsync(_auth, _client, "invalid");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Reserve_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageReservationTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, "reserve",
            productId: Guid.NewGuid().ToString(), warehouseId: Guid.NewGuid().ToString(), quantity: 5);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
