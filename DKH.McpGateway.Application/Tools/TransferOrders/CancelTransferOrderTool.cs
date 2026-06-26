using DKH.WarehouseService.Contracts.Warehouse.Api.TransferOrder.v1;

namespace DKH.McpGateway.Application.Tools.TransferOrders;

[McpServerToolType]
public static class CancelTransferOrderTool
{
    [McpServerTool(Name = "cancel_transfer_order"), Description(
        "Cancel a transfer order.")]
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

        var order = await client.CancelTransferOrderAsync(
            new CancelTransferOrderRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetTransferOrderTool.MapOrder(order), McpJsonDefaults.Options);
    }
}
