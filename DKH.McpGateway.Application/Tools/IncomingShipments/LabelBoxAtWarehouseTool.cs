using DKH.WarehouseService.Contracts.Warehouse.Api.IncomingShipment.v1;

namespace DKH.McpGateway.Application.Tools.IncomingShipments;

[McpServerToolType]
public static class LabelBoxAtWarehouseTool
{
    [McpServerTool(Name = "label_box_at_warehouse"), Description(
        "Label a specific box within an incoming shipment at the warehouse.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IncomingShipmentService.IncomingShipmentServiceClient client,
        [Description("Incoming shipment ID (GUID)")] string id,
        [Description("Box serial number")] string boxSerial,
        [Description("Label value to apply to the box")] string labelValue,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(boxSerial))
        {
            return McpProtoHelper.FormatError("boxSerial is required");
        }

        if (string.IsNullOrWhiteSpace(labelValue))
        {
            return McpProtoHelper.FormatError("labelValue is required");
        }

        var shipment = await client.LabelBoxAtWarehouseAsync(
            new LabelBoxAtWarehouseRequest
            {
                Id = new GuidValue(id),
                BoxSerial = boxSerial,
                LabelValue = labelValue,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetIncomingShipmentTool.MapShipment(shipment), McpJsonDefaults.Options);
    }
}
