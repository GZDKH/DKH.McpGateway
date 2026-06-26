using DKH.WarehouseService.Contracts.Warehouse.Api.IncomingShipment.v1;

namespace DKH.McpGateway.Application.Tools.IncomingShipments;

[McpServerToolType]
public static class ConfirmBoxLabelledTool
{
    [McpServerTool(Name = "confirm_box_labelled"), Description(
        "Confirm that a specific box in an incoming shipment has been labelled.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IncomingShipmentService.IncomingShipmentServiceClient client,
        [Description("Incoming shipment ID (GUID)")] string id,
        [Description("Box serial number")] string boxSerial,
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

        var shipment = await client.ConfirmBoxLabelledAsync(
            new ConfirmBoxLabelledRequest
            {
                Id = new GuidValue(id),
                BoxSerial = boxSerial,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetIncomingShipmentTool.MapShipment(shipment), McpJsonDefaults.Options);
    }
}
