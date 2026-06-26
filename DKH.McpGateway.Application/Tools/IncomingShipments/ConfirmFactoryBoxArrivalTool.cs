using DKH.WarehouseService.Contracts.Warehouse.Api.IncomingShipment.v1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.IncomingShipments;

[McpServerToolType]
public static class ConfirmFactoryBoxArrivalTool
{
    [McpServerTool(Name = "confirm_factory_box_arrival"), Description(
        "Confirm that factory boxes have arrived for an incoming shipment. " +
        "Provide the list of arrived box serials as a JSON array of strings.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IncomingShipmentService.IncomingShipmentServiceClient client,
        [Description("Incoming shipment ID (GUID)")] string id,
        [Description("Arrived box serial numbers as JSON array of strings, e.g. [\"SN001\",\"SN002\"]")] string arrivedBoxSerials,
        [Description("Arrival date/time in ISO 8601 format (optional)")] string? arrivedAt = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(arrivedBoxSerials))
        {
            return McpProtoHelper.FormatError("arrivedBoxSerials is required");
        }

        List<string>? serials;
        try
        {
            serials = JsonSerializer.Deserialize<List<string>>(arrivedBoxSerials, McpJsonDefaults.Options);
        }
        catch (Exception)
        {
            return McpProtoHelper.FormatError("arrivedBoxSerials JSON is invalid");
        }

        var request = new ConfirmFactoryBoxArrivalRequest
        {
            Id = new GuidValue(id),
        };

        if (serials is not null)
        {
            request.ArrivedBoxSerials.AddRange(serials);
        }

        if (!string.IsNullOrWhiteSpace(arrivedAt) && DateTimeOffset.TryParse(arrivedAt, out var arrivedAtParsed))
        {
            request.ArrivedAt = Timestamp.FromDateTime(arrivedAtParsed.UtcDateTime);
        }

        var shipment = await client.ConfirmFactoryBoxArrivalAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetIncomingShipmentTool.MapShipment(shipment), McpJsonDefaults.Options);
    }
}
