using DKH.WarehouseService.Contracts.Warehouse.Api.IncomingShipment.v1;
using DKH.WarehouseService.Contracts.Warehouse.Models.v1;

namespace DKH.McpGateway.Application.Tools.IncomingShipments;

[McpServerToolType]
public static class GetIncomingShipmentTool
{
    [McpServerTool(Name = "get_incoming_shipment"), Description(
        "Get a single incoming shipment by its ID. " +
        "Returns shipment details including status, box serials, and line items.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IncomingShipmentService.IncomingShipmentServiceClient client,
        [Description("Incoming shipment ID (GUID)")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var shipment = await client.GetIncomingShipmentAsync(
            new GetIncomingShipmentRequest { Id = new GuidValue(id) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapShipment(shipment), McpJsonDefaults.Options);
    }

    internal static object MapShipment(IncomingShipmentModel s) => new
    {
        id = s.Id?.Value,
        warehouseId = s.WarehouseId?.Value,
        purchaseOrderId = s.PurchaseOrderId?.Value,
        boxCodeMode = s.BoxCodeMode.ToString(),
        status = s.Status.ToString(),
        lineItems = s.LineItems.Select(li => new
        {
            skuId = li.SkuId?.Value,
            expectedQuantity = li.ExpectedQuantity,
        }),
        expectedBoxSerials = s.ExpectedBoxSerials,
        receivedAt = s.ReceivedAt?.ToDateTimeOffset().ToString("O"),
        createdAt = s.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = s.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
