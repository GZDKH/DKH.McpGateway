using System.Security.Claims;
using System.Security.Cryptography;
using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v2;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using DKH.McpGateway.Application.Tools.Inventory;
using DKH.Platform.Grpc.Common.Types;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Tests.Tools.Inventory;

internal sealed class InventoryToolFixture
{
    internal static readonly string WorkspaceId = "495b9600-23bb-4cee-a833-d86e362e5385";
    internal static readonly string OwnerId = "47b6b12d-635e-4a8f-8698-696106ae50b1";
    internal static readonly string WarehouseId = "ab04bed2-dc11-4fe8-96b4-d7de86495dfb";
    internal static readonly string SellableId = "94a3899f-c01f-4a7f-ad60-e1970adb6726";
    internal static readonly string ReserveId = "76742078-b273-42f6-b6f0-38e0d7432dda";
    internal static readonly string MutationId = "a799ff16-c132-4197-8512-d0b9bbff0459";
    internal static readonly string HoldId = "03bfbfbc-c346-4e07-9980-cbf71311a875";

    internal InventoryToolFixture()
    {
        Http.User = Principal();
        Http.Request.Headers.Authorization = "Bearer controlled-test-token-not-a-real-grant";
        Http.Request.Headers["X-Workspace-Id"] = WorkspaceId;
        Accessor.HttpContext = Http;
        Client = new StockReservationService.StockReservationServiceClient(Transport);
    }

    internal IApiKeyContext Auth { get; } = ApiKeyContextMocks.FullAccess();

    internal DefaultHttpContext Http { get; } = new();

    internal HttpContextAccessor Accessor { get; } = new();

    internal InventoryCallInvoker Transport { get; } = new();

    internal StockReservationService.StockReservationServiceClient Client { get; }

    internal static ClaimsPrincipal Principal() => new(new ClaimsIdentity([
        new Claim("sub", "2d2331a8-6771-4282-9dd0-b62d81931215"), new Claim("iss", "https://issuer.example.test/realms/test"),
        new Claim("preferred_username", "controlled-human"),
        new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)),
    ], "ControlledUnitIdentity"));

    internal static ExactStockScopeModel Scope() => new()
    {
        WorkspaceId = new GuidValue(WorkspaceId),
        OwnerCounterpartyId = new GuidValue(OwnerId),
        WarehouseId = new GuidValue(WarehouseId),
        SellableUnitId = new GuidValue(SellableId),
    };

    internal static OriginalExactReservationModel Original()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(/*lang=json,strict*/ """{ "controlledCart": "茶", "literal": " exact bytes " }""");
        return new OriginalExactReservationModel
        {
            OriginalReserveOperationId = new GuidValue(ReserveId),
            StockScope = Scope(),
            ChildCheckoutOperationId = new GuidValue("47d78e94-5d9f-4c57-b554-98895392a428"),
            CartId = "opaque/cart:literal",
            RawCartLineKey = "unaltered: line / 茶",
            Quantity = 2,
            ExpectedAvailabilityDigest = new string('a', 64),
            OriginalCartFrame = ByteString.CopyFrom(bytes),
            CartFrameSha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
        };
    }

    internal static ExactReservationModel Reservation(OriginalExactReservationModel? original = null)
    {
        original ??= Original();
        return new ExactReservationModel
        {
            HoldId = new GuidValue(HoldId),
            ReserveOperationId = original.OriginalReserveOperationId.Clone(),
            ChildCheckoutOperationId = original.ChildCheckoutOperationId.Clone(),
            StockScope = original.StockScope.Clone(),
            CartId = original.CartId,
            RawCartLineKey = original.RawCartLineKey,
            OriginalQuantity = original.Quantity,
            HoldRevision = 7,
            ReservedQuantity = 2,
            State = ExactReservationStateModel.Active,
        };
    }

    internal static ExactReservationOperationModel Operation(string kind = "release", string? operationId = null) => new()
    {
        OperationId = new GuidValue(operationId ?? MutationId),
        OperationType = kind,
        Reservation = Reservation(),
    };

    internal Task<string> QueryAsync(string action = "availability", int? quantity = null, CancellationToken cancellationToken = default)
        => QueryStockTool.ExecuteAsync(Auth, Client, Accessor, action, WorkspaceId, OwnerId, WarehouseId, SellableId, quantity, cancellationToken);

    internal Task<string> ReservationAsync(string action, IMessage request, CancellationToken cancellationToken = default)
        => ManageReservationTool.ExecuteAsync(Auth, Client, Accessor, action, JsonFormatter.Default.Format(request), cancellationToken);
}
