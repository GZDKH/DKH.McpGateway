using DKH.DeliveryService.Contracts.Delivery.Api.Claims.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.DeliveryClaims;

[McpServerToolType]
public static class OpenDeliveryClaimTool
{
    [McpServerTool(Name = "open_delivery_claim"), Description(
        "Open a delivery claim (loss, damage, delay) against a shipment. " +
        "Returns the created claim with ID and status.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ClaimsService.ClaimsServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("Shipment ID (GUID)")] string shipmentId,
        [Description("Claim type (e.g. Loss, Damage, Delay)")] string type,
        [Description("Amount claimed as a number")] double amountClaimed,
        [Description("Currency code (e.g. USD)")] string currency,
        [Description("Claim description")] string description,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            return McpProtoHelper.FormatError("shipmentId is required");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            return McpProtoHelper.FormatError("type is required");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return McpProtoHelper.FormatError("currency is required");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return McpProtoHelper.FormatError("description is required");
        }

        var request = new OpenClaimRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
            ShipmentId = new GuidValue(shipmentId),
            Type = Enum.Parse<ClaimType>(type, ignoreCase: true),
            AmountClaimed = amountClaimed,
            Currency = currency,
            Description = description,
        };

        var claim = await client.OpenClaimAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapClaim(claim), McpJsonDefaults.Options);
    }

    internal static object MapClaim(ClaimModel c) => new
    {
        id = c.Id?.Value,
        fulfillmentId = c.FulfillmentId?.Value,
        shipmentId = c.ShipmentId?.Value,
        type = c.Type.ToString(),
        amountClaimed = c.AmountClaimed,
        currency = c.Currency,
        description = c.Description,
        status = c.Status.ToString(),
        evidence = c.Evidence.Select(e => new
        {
            id = e.Id?.Value,
            documentRef = e.DocumentRef,
            description = e.Description,
            attachedAt = e.AttachedAt?.ToDateTimeOffset().ToString("O"),
        }),
        openedAt = c.OpenedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = c.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
