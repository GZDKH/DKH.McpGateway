using DKH.DeliveryService.Contracts.Delivery.Api.DeliveryCrud.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class GetShipmentTool
{
    [McpServerTool(Name = "get_shipment"), Description(
        "Get a single shipment by its ID. " +
        "Returns shipment details including carrier, tracking number and URL, and timestamps.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeliveryCrudService.DeliveryCrudServiceClient client,
        [Description("Shipment ID (GUID)")] string shipmentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            return McpProtoHelper.FormatError("shipmentId is required");
        }

        var shipment = await client.GetShipmentAsync(
            new GetShipmentRequest { Id = new GuidValue(shipmentId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapShipment(shipment), McpJsonDefaults.Options);
    }

    /// <summary>
    /// Maps a ShipmentModel to a safe anonymous object.
    /// Reused by ListShipmentsTool.
    /// </summary>
    internal static object MapShipment(ShipmentModel s) => new
    {
        id = s.Id?.Value,
        tenantId = s.TenantId?.Value,
        fulfillmentId = s.FulfillmentId?.Value,
        status = s.Status.ToString(),
        carrierCounterpartyId = s.CarrierCounterpartyId?.Value,
        lastMileCourierCounterpartyId = s.LastMileCourierCounterpartyId?.Value,
        trackingNumber = s.TrackingNumber,
        trackingUrl = s.TrackingUrl,
        shippedAt = s.ShippedAt?.ToDateTimeOffset().ToString("O"),
        deliveredAt = s.DeliveredAt?.ToDateTimeOffset().ToString("O"),
        createdAt = s.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = s.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
