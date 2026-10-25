using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v2;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class ManageReservationTool
{
    [McpServerTool(Name = "manage_reservation"), Description(
        "Exact Inventory operations: reserve, get, get_operation, release, bind_order, confirm. " +
        "json is the corresponding native v2 protobuf request with genuine original Cart/checkout scope, " +
        "literal base64 originalCartFrame and cartFrameSha256. Reserve uses its original operation ID and absent CAS; " +
        "mutations require an actual positive expectedHoldRevision. Confirm means explicit partial physical allocation dispatch, " +
        "not Order completion. No generated IDs, client TTL, retries or fake Cart/Order facts. " +
        "Original HUMAN HTTP bearer/current native mandate required. Legacy list and owner expiry execution are unsupported.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StockReservationService.StockReservationServiceClient client,
        IHttpContextAccessor httpContextAccessor,
        [Description("Action: reserve, get, get_operation, release, bind_order, or confirm")] string action,
        [Description("Strict corresponding native v2 request JSON; original Cart bytes are base64, positive int64 CAS is a string")] string? json = null,
        CancellationToken cancellationToken = default)
    {
        var operation = action.ToLowerInvariant();
        using var context = StockRequestContext.Capture(apiKeyContext, httpContextAccessor,
            operation is "get" or "get_operation" or "list" ? McpPermissions.Read : McpPermissions.Write, cancellationToken);
        if (operation is "list" or "expire")
        {
            return McpProtoHelper.FormatError(StockInput.RetiredMessage + " Expiry is a separate qualified owner operation; this tool never executes it.");
        }

        OriginalExactReservationModel original;
        ExactReservationOperationModel response;
        GuidValue operationId;
        GuidValue? holdId = null;
        string? expectedKind = null;
        switch (operation)
        {
            case "get":
                {
                    var request = StockInput.Parse<GetExactReservationRequest>(json);
                    StockInput.RequireOriginal(request.Original, context);
                    var reservation = await context.InvokeAsync((headers, deadline, token) => client.GetExactReservationAsync(request, headers, deadline, token));
                    StockInput.RequireJoined(reservation, request.Original);
                    return StockInput.Formatter.Format(reservation);
                }
            case "get_operation":
                {
                    var request = StockInput.Parse<GetExactReservationOperationRequest>(json);
                    StockInput.RequireOriginal(request.Original, context);
                    StockInput.RequiredGuid(request.OperationId?.Value, "operationId");
                    original = request.Original;
                    operationId = request.OperationId ?? throw StockInput.Invalid("operationId is required.");
                    response = await context.InvokeAsync((headers, deadline, token) => client.GetExactReservationOperationAsync(request, headers, deadline, token));
                    break;
                }
            case "reserve":
                {
                    var request = StockInput.Parse<ReserveExactStockRequest>(json);
                    RequireMutation(request.Original, request.Command, context, reserve: true);
                    original = request.Original;
                    operationId = request.Command.OperationId;
                    expectedKind = "reserve";
                    response = await context.InvokeAsync((headers, deadline, token) => client.ReserveExactStockAsync(request, headers, deadline, token));
                    break;
                }
            case "release":
                {
                    var request = StockInput.Parse<ReleaseExactReservationRequest>(json);
                    RequireMutation(request.Original, request.Command, context);
                    StockInput.RequiredGuid(request.HoldId?.Value, "holdId");
                    original = request.Original;
                    operationId = request.Command.OperationId;
                    holdId = request.HoldId;
                    expectedKind = "release";
                    response = await context.InvokeAsync((headers, deadline, token) => client.ReleaseExactReservationAsync(request, headers, deadline, token));
                    break;
                }
            case "bind_order":
                {
                    var request = StockInput.Parse<BindExactReservationOrderLineRequest>(json);
                    RequireMutation(request.Original, request.Command, context);
                    StockInput.RequiredGuid(request.HoldId?.Value, "holdId");
                    StockInput.RequiredGuid(request.SelectedSourceLineId?.Value, "selectedSourceLineId");
                    RequireOrderSource(request.OrderSource);
                    original = request.Original;
                    operationId = request.Command.OperationId;
                    holdId = request.HoldId;
                    expectedKind = "bind_order";
                    response = await context.InvokeAsync((headers, deadline, token) => client.BindExactReservationOrderLineAsync(request, headers, deadline, token));
                    break;
                }
            case "confirm":
                {
                    var request = StockInput.Parse<ConfirmExactReservationRequest>(json);
                    RequireMutation(request.Original, request.Command, context);
                    StockInput.RequiredGuid(request.HoldId?.Value, "holdId");
                    StockInput.RequiredGuid(request.OrderLineBindingId?.Value, "orderLineBindingId");
                    if (request.Allocations.Count == 0 || request.Allocations.Any(a => a.Quantity <= 0)
                        || request.Allocations.Select(a => StockInput.RequiredGuid(a.AllocationId?.Value, "allocationId")).Distinct().Count() != request.Allocations.Count)
                    {
                        throw StockInput.Invalid("Confirm requires distinct actual allocation IDs and positive explicit dispatch quantities.");
                    }

                    original = request.Original;
                    operationId = request.Command.OperationId;
                    holdId = request.HoldId;
                    expectedKind = "dispatch";
                    response = await context.InvokeAsync((headers, deadline, token) => client.ConfirmExactReservationAsync(request, headers, deadline, token));
                    break;
                }
            default:
                return McpProtoHelper.FormatError("Unknown action. Use reserve, get, get_operation, release, bind_order or confirm with the corresponding exact native request.");
        }

        StockInput.RequireJoined(response.Reservation, original);
        if (!StockInput.SameId(response.OperationId, operationId) || (expectedKind is not null && response.OperationType != expectedKind)
            || (holdId is not null && !StockInput.SameId(response.Reservation.HoldId, holdId)))
        {
            throw new RpcException(new Status(StatusCode.DataLoss, "Inventory response did not match the requested operation and hold identity."));
        }

        return StockInput.Formatter.Format(response);
    }

    private static void RequireMutation(OriginalExactReservationModel original, ExactReservationCommandModel command, StockRequestContext context, bool reserve = false)
    {
        StockInput.RequireOriginal(original, context);
        StockInput.RequireCommand(command, original, reserve);
    }

    private static void RequireOrderSource(OrderSourceSelectorModel? source)
    {
        StockInput.RequiredGuid(source?.EventId?.Value, "orderSource.eventId");
        StockInput.RequiredGuid(source?.WorkspaceId?.Value, "orderSource.workspaceId");
        StockInput.RequiredGuid(source?.StorefrontId?.Value, "orderSource.storefrontId");
        StockInput.RequiredGuid(source?.OrderId?.Value, "orderSource.orderId");
        if (source is null || source.AggregateRevision <= 0 || string.IsNullOrWhiteSpace(source.PayloadDigest) || string.IsNullOrWhiteSpace(source.MessageDigest))
        {
            throw StockInput.Invalid("The actual original Order source revision and separate payload/message digests are required.");
        }
    }
}
