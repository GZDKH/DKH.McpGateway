using System.Security.Cryptography;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using Google.Protobuf;
using Grpc.Core;

namespace DKH.McpGateway.Application.Tools.Inventory;

/// <summary>Validates transport identity without creating owner authority or source facts.</summary>
internal static class StockInput
{
    internal const string RetiredMessage = "This legacy inventory operation is retired and has no faithful Inventory 1.19 equivalent. Use query_stock availability with an exact stock Workspace/owner counterparty/warehouse/Sellable scope, or manage_reservation with the genuine original Cart frame and native operation/CAS. Product/Variant IDs, generic stock pages, direct adjustments and alerts cannot be substituted.";

    internal static readonly JsonFormatter Formatter = new(JsonFormatter.Settings.Default.WithFormatDefaultValues(true));

    internal static Guid RequiredGuid(string? value, string name)
        => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : throw Invalid($"{name} must be a non-empty GUID.");

    internal static void RequireScope(ExactStockScopeModel? scope, StockRequestContext context)
    {
        if (scope is null || RequiredGuid(scope.WorkspaceId?.Value, "stockScope.workspaceId") != context.WorkspaceId)
        {
            throw Invalid("stockScope.workspaceId must equal the original selected X-Workspace-Id.");
        }

        RequiredGuid(scope.OwnerCounterpartyId?.Value, "stockScope.ownerCounterpartyId");
        RequiredGuid(scope.WarehouseId?.Value, "stockScope.warehouseId");
        RequiredGuid(scope.SellableUnitId?.Value, "stockScope.sellableUnitId");
    }

    internal static T Parse<T>(string? json) where T : IMessage<T>, new()
    {
        if (string.IsNullOrWhiteSpace(json) || System.Text.Encoding.UTF8.GetByteCount(json) > 2 * 1024 * 1024)
        {
            throw Invalid("A native exact request JSON object of at most 2 MiB is required.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            RequireExactRevisions(document.RootElement);
            return JsonParser.Default.Parse<T>(json);
        }
        catch (Exception exception) when (exception is InvalidProtocolBufferException or InvalidJsonException or FormatException or JsonException)
        {
            throw Invalid("Invalid exact Inventory request JSON; unknown or legacy fields are not accepted.");
        }
    }

    private static void RequireExactRevisions(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Invalid("A native exact request JSON object is required.");
        }

        foreach (var property in root.EnumerateObject())
        {
            var revision = property.Name switch
            {
                "command" => "expectedHoldRevision",
                "orderSource" or "order_source" => "aggregateRevision",
                _ => null,
            };
            if (revision is null || property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var field in property.Value.EnumerateObject())
            {
                if ((field.Name == revision || field.Name is "expected_hold_revision" or "aggregate_revision")
                    && field.Value.ValueKind != JsonValueKind.String)
                {
                    throw Invalid("Native int64 revision values must be exact protobuf JSON strings, never floating-point numbers.");
                }
            }
        }
    }

    internal static void RequireOriginal(OriginalExactReservationModel? original, StockRequestContext context)
    {
        if (original is null)
        {
            throw Invalid("The genuine original Cart reservation frame is required.");
        }

        RequireScope(original.StockScope, context);
        RequiredGuid(original.OriginalReserveOperationId?.Value, "originalReserveOperationId");
        RequiredGuid(original.ChildCheckoutOperationId?.Value, "childCheckoutOperationId");
        if (string.IsNullOrWhiteSpace(original.CartId) || string.IsNullOrWhiteSpace(original.RawCartLineKey)
            || original.Quantity <= 0 || string.IsNullOrWhiteSpace(original.ExpectedAvailabilityDigest)
            || original.OriginalCartFrame.Length is < 1 or > 1024 * 1024
            || !string.Equals(Convert.ToHexStringLower(SHA256.HashData(original.OriginalCartFrame.Span)), original.CartFrameSha256, StringComparison.Ordinal))
        {
            throw Invalid("Original Cart identity, positive quantity, availability digest and exact Cart bytes/SHA256 are required. These are DATA, not stock authority.");
        }
    }

    internal static void RequireCommand(ExactReservationCommandModel? command, OriginalExactReservationModel original, bool reserve)
    {
        var operation = RequiredGuid(command?.OperationId?.Value, "command.operationId");
        if (command is null || (reserve
            ? command.HasExpectedHoldRevision || operation != RequiredGuid(original.OriginalReserveOperationId?.Value, "originalReserveOperationId")
            : !command.HasExpectedHoldRevision || command.ExpectedHoldRevision <= 0))
        {
            throw Invalid("Reserve requires the original reserve operation ID and absent CAS; later mutations require the actual positive expectedHoldRevision.");
        }
    }

    internal static void RequireJoined(ExactReservationModel? result, OriginalExactReservationModel original)
    {
        if (result is null || !Guid.TryParse(result.HoldId?.Value, out var holdId) || holdId == Guid.Empty || result.HoldRevision <= 0
            || !SameScope(result.StockScope, original.StockScope)
            || !SameId(result.ReserveOperationId, original.OriginalReserveOperationId)
            || !SameId(result.ChildCheckoutOperationId, original.ChildCheckoutOperationId)
            || result.CartId != original.CartId || result.RawCartLineKey != original.RawCartLineKey
            || result.OriginalQuantity != original.Quantity)
        {
            throw new RpcException(new Status(StatusCode.DataLoss, "Inventory response did not match the requested original stock scope and Cart identity."));
        }
    }

    internal static bool SameScope(ExactStockScopeModel? a, ExactStockScopeModel? b)
        => a is not null && b is not null && SameId(a.WorkspaceId, b.WorkspaceId)
            && SameId(a.OwnerCounterpartyId, b.OwnerCounterpartyId) && SameId(a.WarehouseId, b.WarehouseId)
            && SameId(a.SellableUnitId, b.SellableUnitId);

    internal static bool SameId(GuidValue? a, GuidValue? b)
        => Guid.TryParse(a?.Value, out var x) && x != Guid.Empty && Guid.TryParse(b?.Value, out var y) && x == y;

    internal static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
