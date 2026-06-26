using DKH.WarehouseService.Contracts.Warehouse.Api.IncomingShipment.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.IncomingShipments;

[McpServerToolType]
public static class ListIncomingShipmentsTool
{
    [McpServerTool(Name = "list_incoming_shipments"), Description(
        "List incoming shipments with optional filtering by warehouseId, purchaseOrderId, or status. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IncomingShipmentService.IncomingShipmentServiceClient client,
        [Description("Warehouse ID (GUID, optional)")] string? warehouseId = null,
        [Description("Purchase order ID (GUID, optional)")] string? purchaseOrderId = null,
        [Description("Status filter (Pending, InTransit, Arrived, Labelled, Closed, Cancelled — optional)")] string? status = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListIncomingShipmentsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            request.WarehouseId = new GuidValue(warehouseId);
        }

        if (!string.IsNullOrWhiteSpace(purchaseOrderId))
        {
            request.PurchaseOrderId = new GuidValue(purchaseOrderId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<IncomingShipmentStatus>(status, ignoreCase: true);
        }

        var response = await client.ListIncomingShipmentsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetIncomingShipmentTool.MapShipment),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
