using DKH.WarehouseService.Contracts.Warehouse.Api.TransferOrder.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.TransferOrders;

[McpServerToolType]
public static class GetTransferOrderTool
{
    [McpServerTool(Name = "get_transfer_order"), Description(
        "Get a single transfer order by its ID. " +
        "Returns transfer order details including origin/destination stores, status, line items, and timestamps.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TransferOrderService.TransferOrderServiceClient client,
        [Description("Transfer order ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var order = await client.GetTransferOrderAsync(
            new GetTransferOrderRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapOrder(order), McpJsonDefaults.Options);
    }

    internal static object MapOrder(TransferOrderModel o) => new
    {
        id = o.Id?.Value,
        originStoreId = o.OriginStoreId?.Value,
        destinationStoreId = o.DestinationStoreId?.Value,
        status = o.Status.ToString(),
        lineItems = o.LineItems.Select(li => new
        {
            sku = li.Sku,
            quantity = li.Quantity,
        }),
        createdAt = o.CreatedAt?.ToDateTimeOffset().ToString("O"),
        sentAt = o.SentAt?.ToDateTimeOffset().ToString("O"),
        receivedAt = o.ReceivedAt?.ToDateTimeOffset().ToString("O"),
        cancelledAt = o.CancelledAt?.ToDateTimeOffset().ToString("O"),
    };
}
