using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v2;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class QueryStockTool
{
    [McpServerTool(Name = "query_stock"), Description(
        "Read exact current stock availability. Actions: availability or check_availability. " +
        "Requires actual stock workspaceId, ownerCounterpartyId, warehouseId and sellableUnitId, " +
        "original HTTP HUMAN bearer and X-Workspace-Id. Native current owner mandate is independent. " +
        "Product/Variant lookup and generic on-hand/reserved pages are retired; never infer stock scope.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StockReservationService.StockReservationServiceClient client,
        IHttpContextAccessor httpContextAccessor,
        [Description("Action: availability or check_availability; old get_level/get_levels are retired")] string action,
        [Description("Actual stock-owner Workspace GUID, matching selected X-Workspace-Id")] string? workspaceId = null,
        [Description("Actual stock-owner counterparty GUID")] string? ownerCounterpartyId = null,
        [Description("Actual stock warehouse GUID")] string? warehouseId = null,
        [Description("Actual exact SellableUnit GUID, never Product/Variant/CatalogSellable ID")] string? sellableUnitId = null,
        [Description("Positive requested quantity for check_availability")] int? quantity = null,
        CancellationToken cancellationToken = default)
    {
        using var context = StockRequestContext.Capture(apiKeyContext, httpContextAccessor, McpPermissions.Read, cancellationToken);
        var operation = action.ToLowerInvariant();
        if (operation is "get_level" or "get_levels")
        {
            return McpProtoHelper.FormatError(StockInput.RetiredMessage);
        }

        if (operation is not ("availability" or "check_availability"))
        {
            return McpProtoHelper.FormatError("Unknown action. Use availability or check_availability with the exact stock scope.");
        }

        var scope = new ExactStockScopeModel
        {
            WorkspaceId = new GuidValue(StockInput.RequiredGuid(workspaceId, "workspaceId").ToString("D")),
            OwnerCounterpartyId = new GuidValue(StockInput.RequiredGuid(ownerCounterpartyId, "ownerCounterpartyId").ToString("D")),
            WarehouseId = new GuidValue(StockInput.RequiredGuid(warehouseId, "warehouseId").ToString("D")),
            SellableUnitId = new GuidValue(StockInput.RequiredGuid(sellableUnitId, "sellableUnitId").ToString("D")),
        };
        StockInput.RequireScope(scope, context);
        if (operation == "check_availability" && (!quantity.HasValue || quantity.Value <= 0))
        {
            throw StockInput.Invalid("quantity must be a positive integer for check_availability.");
        }

        var response = await context.InvokeAsync((headers, deadline, token) => client.GetExactStockAvailabilityAsync(
            new GetExactStockAvailabilityRequest { StockScope = scope }, headers, deadline, token));
        if (!StockInput.SameScope(response.StockScope, scope) || response.AvailableQuantity < 0)
        {
            throw new RpcException(new Status(StatusCode.DataLoss, "Inventory availability did not match the requested exact stock scope."));
        }

        return JsonSerializer.Serialize(new
        {
            stockScope = JsonSerializer.Deserialize<JsonElement>(StockInput.Formatter.Format(response.StockScope)),
            stockId = response.StockId?.Value,
            availableQuantity = response.AvailableQuantity,
            availabilityDigest = response.AvailabilityDigest,
            isAvailable = operation == "check_availability" ? response.AvailableQuantity >= quantity!.Value : (bool?)null,
        }, McpJsonDefaults.Options);
    }
}
