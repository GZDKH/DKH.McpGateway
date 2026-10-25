using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v2;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using DKH.McpGateway.Application.Tools.Inventory;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class QueryStockToolTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(3, true)]
    public async Task CheckAvailability_PreservesGenuineZeroAndExactScopeAsync(int available, bool expected)
    {
        var f = new InventoryToolFixture();
        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(new ExactStockAvailabilityModel
        {
            StockScope = InventoryToolFixture.Scope(),
            AvailableQuantity = available,
            AvailabilityDigest = "opaque-owner-digest",
            StockId = new(InventoryToolFixture.HoldId),
        });
        var result = JsonDocument.Parse(await f.QueryAsync("check_availability", 2)).RootElement;
        result.GetProperty("availableQuantity").GetInt32().Should().Be(available);
        result.GetProperty("isAvailable").GetBoolean().Should().Be(expected);
        f.Transport.Calls.Should().ContainSingle();
        var call = f.Transport.Calls[0];
        call.Path.Should().Be("/proto.inventory.api.stock_reservation.v2.StockReservationService/GetExactStockAvailability");
        ((GetExactStockAvailabilityRequest)call.Request).StockScope.Should().Be(InventoryToolFixture.Scope());
        call.Options.Headers!.Single(x => x.Key == "x-workspace-id").Value.Should().Be(InventoryToolFixture.WorkspaceId);
        call.Options.Deadline.Should().BeBefore(DateTime.UtcNow.AddHours(2));
    }

    [Theory]
    [InlineData("get_level")]
    [InlineData("get_levels")]
    public async Task LegacyProductQueries_AreExplicitlyRetiredWithoutNativeCallAsync(string action)
    {
        var f = new InventoryToolFixture();
        (await f.QueryAsync(action)).Should().Contain("retired");
        f.Transport.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task MissingOrInvalidOwner_DoesNotInferOwnerAsync(string? owner)
    {
        var f = new InventoryToolFixture();
        var act = () => QueryStockTool.ExecuteAsync(f.Auth, f.Client, f.Accessor, "availability",
            InventoryToolFixture.WorkspaceId, owner, InventoryToolFixture.WarehouseId, InventoryToolFixture.SellableId);
        await act.Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.InvalidArgument);
        f.Transport.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CrossWorkspace_DoesNotCallInventoryAsync()
    {
        var f = new InventoryToolFixture();
        f.Http.Request.Headers["X-Workspace-Id"] = Guid.NewGuid().ToString();
        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<RpcException>();
        f.Transport.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ForeignOwnerResponse_IsNotDisclosedAsync()
    {
        var f = new InventoryToolFixture();
        var scope = InventoryToolFixture.Scope();
        scope.OwnerCounterpartyId = new(Guid.NewGuid().ToString());
        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(new ExactStockAvailabilityModel { StockScope = scope, AvailableQuantity = 17 });
        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.DataLoss);
    }

    [Theory]
    [InlineData(StatusCode.NotFound)]
    [InlineData(StatusCode.PermissionDenied)]
    [InlineData(StatusCode.Unavailable)]
    public async Task NativeRefusal_IsNotInventedZeroOrSuccessAsync(StatusCode status)
    {
        var f = new InventoryToolFixture();
        f.Transport.RespondAsync = (_, _) => Task.FromException<object>(new RpcException(new Status(status, "controlled native refusal")));
        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == status);
        f.Transport.Calls.Should().ContainSingle();
    }
}
