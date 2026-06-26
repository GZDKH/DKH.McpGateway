using DKH.WarehouseService.Contracts.Warehouse.Api.TransferOrder.v1;

namespace DKH.McpGateway.Application.Tools.TransferOrders;

[McpServerToolType]
public static class ReceiveTransferOrderTool
{
    [McpServerTool(Name = "receive_transfer_order"), Description(
        "Confirm receipt of a transfer order at the destination store.")]
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

        var order = await client.ReceiveTransferOrderAsync(
            new ReceiveTransferOrderRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetTransferOrderTool.MapOrder(order), McpJsonDefaults.Options);
    }
}
