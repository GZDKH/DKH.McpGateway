using DKH.WarehouseService.Contracts.Warehouse.Api.TransferOrder.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.TransferOrders;

[McpServerToolType]
public static class CreateTransferOrderTool
{
    [McpServerTool(Name = "create_transfer_order"), Description(
        "Create a new transfer order between two stores. " +
        "lineItems should be a JSON array of {\"sku\":\"SKU001\",\"quantity\":10} objects.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TransferOrderService.TransferOrderServiceClient client,
        [Description("Origin store ID (GUID)")] string originStoreId,
        [Description("Destination store ID (GUID)")] string destinationStoreId,
        [Description("Line items as JSON: [{\"sku\":\"SKU001\",\"quantity\":10}]")] string lineItems,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(originStoreId))
        {
            return McpProtoHelper.FormatError("originStoreId is required");
        }

        if (string.IsNullOrWhiteSpace(destinationStoreId))
        {
            return McpProtoHelper.FormatError("destinationStoreId is required");
        }

        if (string.IsNullOrWhiteSpace(lineItems))
        {
            return McpProtoHelper.FormatError("lineItems is required");
        }

        List<JsonElement>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<JsonElement>>(lineItems, McpJsonDefaults.Options);
        }
        catch (Exception)
        {
            return McpProtoHelper.FormatError("lineItems JSON is invalid");
        }

        var request = new CreateTransferOrderRequest
        {
            OriginStoreId = new GuidValue(originStoreId),
            DestinationStoreId = new GuidValue(destinationStoreId),
        };

        if (items is not null)
        {
            foreach (var item in items)
            {
                request.LineItems.Add(new TransferOrderLineItem
                {
                    Sku = item.GetProperty("sku").GetString() ?? "",
                    Quantity = item.GetProperty("quantity").GetInt32(),
                });
            }
        }

        var order = await client.CreateTransferOrderAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetTransferOrderTool.MapOrder(order), McpJsonDefaults.Options);
    }
}
