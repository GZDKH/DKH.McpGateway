using DKH.DeliveryService.Contracts.Delivery.Api.DeliveryCrud.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class ListShipmentsTool
{
    [McpServerTool(Name = "list_shipments"), Description(
        "List shipments with optional filtering. " +
        "Filter by fulfillmentId, status, or carrierCounterpartyId. " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeliveryCrudService.DeliveryCrudServiceClient client,
        [Description("Fulfillment ID (GUID, optional)")] string? fulfillmentId = null,
        [Description("Shipment status filter (e.g. Pending, Shipped, Delivered)")] string? status = null,
        [Description("Carrier counterparty ID (GUID, optional)")] string? carrierCounterpartyId = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListShipmentsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(fulfillmentId))
        {
            request.FulfillmentId = new GuidValue(fulfillmentId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<ShipmentStatus>(status, ignoreCase: true);
        }

        if (!string.IsNullOrWhiteSpace(carrierCounterpartyId))
        {
            request.CarrierCounterpartyId = new GuidValue(carrierCounterpartyId);
        }

        var response = await client.ListShipmentsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetShipmentTool.MapShipment),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
