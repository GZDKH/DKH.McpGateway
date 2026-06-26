using DKH.WarehouseService.Contracts.Warehouse.Api.TransferOrder.v1;

namespace DKH.McpGateway.Application.Tools.TransferOrders;

[McpServerToolType]
public static class MarkTransferOrderInTransitTool
{
    [McpServerTool(Name = "mark_transfer_order_in_transit"), Description(
        "Mark a transfer order as in-transit, indicating it has been dispatched from the origin store.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TransferOrderService.TransferOrderServiceClient client,
        [Description("Transfer order ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var order = await client.MarkTransferOrderInTransitAsync(
            new MarkTransferOrderInTransitRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetTransferOrderTool.MapOrder(order), McpJsonDefaults.Options);
    }
}
