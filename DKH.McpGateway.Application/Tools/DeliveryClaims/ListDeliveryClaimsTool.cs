using DKH.DeliveryService.Contracts.Delivery.Api.Claims.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.DeliveryClaims;

[McpServerToolType]
public static class ListDeliveryClaimsTool
{
    [McpServerTool(Name = "list_delivery_claims"), Description(
        "List delivery claims with optional filtering by fulfillment, shipment, or status.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ClaimsService.ClaimsServiceClient client,
        [Description("Fulfillment ID (GUID, optional)")] string? fulfillmentId = null,
        [Description("Shipment ID (GUID, optional)")] string? shipmentId = null,
        [Description("Claim status filter (e.g. Open, UnderReview, Approved, Rejected, Settled)")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListClaimsRequest();

        if (!string.IsNullOrWhiteSpace(fulfillmentId))
        {
            request.FulfillmentId = new GuidValue(fulfillmentId);
        }

        if (!string.IsNullOrWhiteSpace(shipmentId))
        {
            request.ShipmentId = new GuidValue(shipmentId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            request.Status = Enum.Parse<ClaimStatus>(status, ignoreCase: true);
        }

        var response = await client.ListClaimsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(OpenDeliveryClaimTool.MapClaim),
        }, McpJsonDefaults.Options);
    }
}
