using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v1;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v1;

namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class ManageReservationTool
{
    [McpServerTool(Name = "manage_reservation"), Description(
        "Manage stock reservations: reserve, release, confirm, get, or list. " +
        "For reserve: provide productId, warehouseId, quantity, optional variantId, cartId, ttlMinutes. " +
        "For release/confirm/get: provide reservationId. " +
        "For list: optional cartId, status (active/confirmed/released/expired), page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StockReservationService.StockReservationServiceClient client,
        [Description("Action: reserve, release, confirm, get, or list")] string action,
        [Description("Reservation ID (GUID, for release/confirm/get)")] string? reservationId = null,
        [Description("Product ID (GUID, for reserve)")] string? productId = null,
        [Description("Variant ID (GUID, optional)")] string? variantId = null,
        [Description("Warehouse ID (GUID, for reserve)")] string? warehouseId = null,
        [Description("Quantity to reserve (for reserve)")] int? quantity = null,
        [Description("Cart ID (for reserve or list filter)")] string? cartId = null,
        [Description("TTL in minutes (for reserve, optional)")] int? ttlMinutes = null,
        [Description("Reservation status filter (for list): active, confirmed, released, expired")] string? status = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "reserve" => await ReserveAsync(client, apiKeyContext, productId, variantId, warehouseId, quantity, cartId, ttlMinutes, cancellationToken),
            "release" => await ReleaseAsync(client, apiKeyContext, reservationId, cancellationToken),
            "confirm" => await ConfirmAsync(client, apiKeyContext, reservationId, cancellationToken),
            "get" => await GetAsync(client, apiKeyContext, reservationId, cancellationToken),
            "list" => await ListAsync(client, apiKeyContext, cartId, status, page, pageSize, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: reserve, release, confirm, get, or list"),
        };
    }

    private static async Task<string> ReserveAsync(
        StockReservationService.StockReservationServiceClient client,
        IApiKeyContext ctx, string? productId, string? variantId, string? warehouseId,
        int? quantity, string? cartId, int? ttlMinutes, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(productId))
        {
            return McpProtoHelper.FormatError("productId is required for reserve");
        }

        if (string.IsNullOrWhiteSpace(warehouseId))
        {
            return McpProtoHelper.FormatError("warehouseId is required for reserve");
        }

        if (!quantity.HasValue || quantity.Value <= 0)
        {
            return McpProtoHelper.FormatError("quantity must be a positive integer for reserve");
        }

        var request = new ReserveStockRequest
        {
            ProductId = new GuidValue(productId),
            WarehouseId = new GuidValue(warehouseId),
            Quantity = quantity.Value,
        };

        if (!string.IsNullOrWhiteSpace(variantId))
        {
            request.VariantId = new GuidValue(variantId);
        }

        if (!string.IsNullOrWhiteSpace(cartId))
        {
            request.CartId = cartId;
        }

        if (ttlMinutes.HasValue)
        {
            request.TtlMinutes = ttlMinutes.Value;
        }

        var result = await client.ReserveStockAsync(request, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(result);
    }

    private static async Task<string> ReleaseAsync(
        StockReservationService.StockReservationServiceClient client,
        IApiKeyContext ctx, string? reservationId, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(reservationId))
        {
            return McpProtoHelper.FormatError("reservationId is required for release");
        }

        await client.ReleaseReservationAsync(
            new ReleaseReservationRequest { ReservationId = new GuidValue(reservationId) },
            cancellationToken: ct);
        return McpProtoHelper.FormatOk();
    }

    private static async Task<string> ConfirmAsync(
        StockReservationService.StockReservationServiceClient client,
        IApiKeyContext ctx, string? reservationId, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(reservationId))
        {
            return McpProtoHelper.FormatError("reservationId is required for confirm");
        }

        await client.ConfirmReservationAsync(
            new ConfirmReservationRequest { ReservationId = new GuidValue(reservationId) },
            cancellationToken: ct);
        return McpProtoHelper.FormatOk();
    }

    private static async Task<string> GetAsync(
        StockReservationService.StockReservationServiceClient client,
        IApiKeyContext ctx, string? reservationId, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(reservationId))
        {
            return McpProtoHelper.FormatError("reservationId is required for get");
        }

        var result = await client.GetReservationAsync(
            new GetReservationRequest { ReservationId = new GuidValue(reservationId) },
            cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(result);
    }

    private static async Task<string> ListAsync(
        StockReservationService.StockReservationServiceClient client,
        IApiKeyContext ctx, string? cartId, string? status, int? page, int? pageSize, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);

        var request = new ListReservationsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(cartId))
        {
            request.CartId = cartId;
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = MapStatus(status);
        }

        var result = await client.ListReservationsAsync(request, cancellationToken: ct);
        return McpProtoHelper.FormatListResponse(
            result.Items,
            result.Metadata.TotalCount,
            result.Metadata.CurrentPage,
            result.Metadata.PageSize);
    }

    private static ReservationStatusModel MapStatus(string status) => status.ToLowerInvariant() switch
    {
        "active" => ReservationStatusModel.Active,
        "confirmed" => ReservationStatusModel.Confirmed,
        "released" => ReservationStatusModel.Released,
        "expired" => ReservationStatusModel.Expired,
        _ => ReservationStatusModel.Unspecified,
    };
}
