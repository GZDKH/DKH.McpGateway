using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v2;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using DKH.McpGateway.Application.Tools.Inventory;
using Google.Protobuf;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class ManageReservationToolTests
{
    [Theory]
    [InlineData("reserve", "ReserveExactStock", "reserve")]
    [InlineData("get", "GetExactReservation", "get")]
    [InlineData("get_operation", "GetExactReservationOperation", "release")]
    [InlineData("release", "ReleaseExactReservation", "release")]
    [InlineData("bind_order", "BindExactReservationOrderLine", "bind_order")]
    [InlineData("confirm", "ConfirmExactReservation", "dispatch")]
    public async Task ExactNativeOperations_PreserveOriginalBytesAndCallerSuppliedIdentityAsync(string action, string rpc, string kind)
    {
        var f = new InventoryToolFixture();
        var request = Request(action);
        var requestBytes = request.ToByteArray();
        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(action == "get"
            ? InventoryToolFixture.Reservation()
            : InventoryToolFixture.Operation(kind, action == "reserve" ? InventoryToolFixture.ReserveId : InventoryToolFixture.MutationId));
        var output = await f.ReservationAsync(action, request);
        output.Should().Contain(InventoryToolFixture.HoldId);
        f.Transport.Calls.Should().ContainSingle();
        var call = f.Transport.Calls[0];
        call.Path.Should().Be("/proto.inventory.api.stock_reservation.v2.StockReservationService/" + rpc);
        call.Bytes.Should().Equal(requestBytes);
        call.Options.Headers!.Single(x => x.Key == "x-workspace-id").Value.Should().Be(InventoryToolFixture.WorkspaceId);
        request.ToByteArray().Should().Equal(requestBytes);
    }

    [Theory]
    [InlineData("release")]
    [InlineData("bind_order")]
    [InlineData("confirm")]
    public async Task MutationMissingOrZeroCas_IsNotGuessedAsync(string action)
    {
        var f = new InventoryToolFixture();
        var json = JsonFormatter.Default.Format(Request(action)).Replace("\"expectedHoldRevision\": \"7\"", "\"expectedHoldRevision\": \"0\"", StringComparison.Ordinal);
        var act = () => ManageReservationTool.ExecuteAsync(f.Auth, f.Client, f.Accessor, action, json);
        await act.Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.InvalidArgument);
        f.Transport.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("release")]
    [InlineData("bind_order")]
    public async Task Int64Revision_IsPassedLosslesslyAsNativeWireValueAsync(string action)
    {
        var f = new InventoryToolFixture();
        var request = Request(action);
        if (request is ReleaseExactReservationRequest release)
        {
            release.Command.ExpectedHoldRevision = long.MaxValue;
        }

        if (request is BindExactReservationOrderLineRequest bind)
        {
            bind.Command.ExpectedHoldRevision = long.MaxValue;
            bind.OrderSource.AggregateRevision = long.MaxValue;
        }

        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(InventoryToolFixture.Operation(action == "bind_order" ? "bind_order" : "release"));
        await f.ReservationAsync(action, request);
        f.Transport.Calls.Single().Bytes.Should().Equal(request.ToByteArray());
    }

    [Theory]
    [InlineData("expectedHoldRevision")]
    [InlineData("expected_hold_revision")]
    public async Task NumericInt64Cas_IsRejectedInsteadOfRoundedAsync(string field)
    {
        var f = new InventoryToolFixture();
        var json = JsonFormatter.Default.Format(Request("release")).Replace("\"expectedHoldRevision\": \"7\"", "\"" + field + "\": 9223372036854775807", StringComparison.Ordinal);
        await ((Func<Task>)(async () => await ManageReservationTool.ExecuteAsync(f.Auth, f.Client, f.Accessor, "release", json))).Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.InvalidArgument);
        f.Transport.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReserveWithMutationCas_IsRejectedAsync()
    {
        var f = new InventoryToolFixture();
        var request = (ReserveExactStockRequest)Request("reserve");
        request.Command.ExpectedHoldRevision = 7;
        await ((Func<Task>)(async () => await f.ReservationAsync("reserve", request))).Should().ThrowAsync<RpcException>();
        f.Transport.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task ReserveCannotReplaceOriginalOperationIdAsync()
    {
        var f = new InventoryToolFixture();
        var request = (ReserveExactStockRequest)Request("reserve");
        request.Command.OperationId = new(InventoryToolFixture.MutationId);
        await ((Func<Task>)(async () => await f.ReservationAsync("reserve", request))).Should().ThrowAsync<RpcException>();
        f.Transport.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("frame")]
    [InlineData("digest")]
    [InlineData("workspace")]
    [InlineData("owner")]
    [InlineData("quantity")]
    public async Task InvalidOriginal_DoesNotGenerateCartOrScopeAsync(string mutation)
    {
        var f = new InventoryToolFixture();
        var request = (ReserveExactStockRequest)Request("reserve");
        switch (mutation)
        {
            case "frame":
                request.Original.OriginalCartFrame = ByteString.CopyFromUtf8("changed");
                break;
            case "digest":
                request.Original.CartFrameSha256 = new string('b', 64);
                break;
            case "workspace":
                request.Original.StockScope.WorkspaceId = new(Guid.NewGuid().ToString());
                break;
            case "owner":
                request.Original.StockScope.OwnerCounterpartyId = null;
                break;
            case "quantity":
                request.Original.Quantity = 0;
                break;
        }

        await ((Func<Task>)(async () => await f.ReservationAsync("reserve", request))).Should().ThrowAsync<RpcException>();
        f.Transport.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData(/*lang=json,strict*/ "{\"productId\": \"legacy-product\"}")]
    [InlineData(/*lang=json,strict*/ "{\"ttlMinutes\": 20}")]
    [InlineData("{broken")]
    public async Task LegacyUnknownOrMalformedJson_IsRejectedWithoutRpcAsync(string json)
    {
        var f = new InventoryToolFixture();
        await ((Func<Task>)(async () => await ManageReservationTool.ExecuteAsync(f.Auth, f.Client, f.Accessor, "reserve", json))).Should().ThrowAsync<RpcException>();
        f.Transport.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("list")]
    [InlineData("expire")]
    public async Task UnsupportedActions_MakeNoLegacyOrExpiryCallAsync(string action)
    {
        var f = new InventoryToolFixture();
        (await ManageReservationTool.ExecuteAsync(f.Auth, f.Client, f.Accessor, action)).Should().Contain("retired");
        f.Transport.Calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("hold")]
    [InlineData("cart")]
    [InlineData("stockOwner")]
    public async Task UnjoinedNativeOutcome_IsNotDisclosedAsync(string mutation)
    {
        var f = new InventoryToolFixture();
        var response = InventoryToolFixture.Operation();
        switch (mutation)
        {
            case "operation":
                response.OperationId = new(Guid.NewGuid().ToString());
                break;
            case "hold":
                response.Reservation.HoldId = new(Guid.NewGuid().ToString());
                break;
            case "cart":
                response.Reservation.CartId = "different-cart";
                break;
            case "stockOwner":
                response.Reservation.StockScope.OwnerCounterpartyId = new(Guid.NewGuid().ToString());
                break;
        }

        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(response);
        await ((Func<Task>)(async () => await f.ReservationAsync("release", Request("release")))).Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.DataLoss);
    }

    [Theory]
    [InlineData(StatusCode.NotFound)]
    [InlineData(StatusCode.PermissionDenied)]
    [InlineData(StatusCode.Unavailable)]
    public async Task AmbiguousOrDeniedMutation_IsNotRetriedOrReportedReleasedAsync(StatusCode status)
    {
        var f = new InventoryToolFixture();
        f.Transport.RespondAsync = (_, _) => Task.FromException<object>(new RpcException(new Status(status, "controlled original outcome")));
        await ((Func<Task>)(async () => await f.ReservationAsync("release", Request("release")))).Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == status);
        f.Transport.Calls.Should().ContainSingle();
    }

    internal static IMessage Request(string action)
    {
        var original = InventoryToolFixture.Original();
        var command = new ExactReservationCommandModel { OperationId = new(InventoryToolFixture.MutationId), ExpectedHoldRevision = 7 };
        return action switch
        {
            "reserve" => new ReserveExactStockRequest { Original = original, Command = new() { OperationId = original.OriginalReserveOperationId.Clone() } },
            "get" => new GetExactReservationRequest { Original = original },
            "get_operation" => new GetExactReservationOperationRequest { Original = original, OperationId = new(InventoryToolFixture.MutationId) },
            "release" => new ReleaseExactReservationRequest { Original = original, HoldId = new(InventoryToolFixture.HoldId), Command = command },
            "bind_order" => new BindExactReservationOrderLineRequest
            {
                Original = original,
                HoldId = new(InventoryToolFixture.HoldId),
                Command = command,
                SelectedSourceLineId = new("fd0f47ae-5ecf-408c-a5d8-357b583f0f28"),
                OrderSource = new()
                {
                    EventId = new("47838f88-8954-4fb6-9f8f-3e5c5295caa3"),
                    WorkspaceId = new("34974343-85e3-4133-9ce6-b642f3747240"),
                    StorefrontId = new("91b93539-1c01-4ebf-be3e-653443bb986a"),
                    OrderId = new("2b610b3a-0279-495e-bab0-267a00698bb2"),
                    AggregateRevision = 13,
                    PayloadDigest = "original-payload",
                    MessageDigest = "original-message"
                },
            },
            "confirm" => new ConfirmExactReservationRequest
            {
                Original = original,
                HoldId = new(InventoryToolFixture.HoldId),
                Command = command,
                OrderLineBindingId = new("326702ba-1efc-4a7a-8910-18adc20b72c7"),
                Allocations = { new ExactReservationDispatchModel { AllocationId = new("e8edbb19-6a78-4f86-9cc3-130088003759"), Quantity = 1 } },
            },
            _ => throw new ArgumentException("Unknown controlled request.", nameof(action)),
        };
    }
}
