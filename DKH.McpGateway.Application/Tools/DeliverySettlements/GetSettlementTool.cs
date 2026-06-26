using DKH.DeliveryService.Contracts.Delivery.Api.Settlements.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.DeliverySettlements;

[McpServerToolType]
public static class GetSettlementTool
{
    [McpServerTool(Name = "get_settlement"), Description(
        "Get the settlement record for a fulfillment. " +
        "Returns settlement lines with types, amounts, statuses, and party IDs.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SettlementsService.SettlementsServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        var settlement = await client.GetSettlementAsync(
            new GetSettlementRequest { FulfillmentId = new GuidValue(fulfillmentId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapSettlement(settlement), McpJsonDefaults.Options);
    }

    internal static object MapSettlement(SettlementRecordModel s) => new
    {
        id = s.Id?.Value,
        fulfillmentId = s.FulfillmentId?.Value,
        shipmentId = s.ShipmentId?.Value,
        lines = s.Lines.Select(l => new
        {
            id = l.Id?.Value,
            type = l.Type.ToString(),
            partyId = l.PartyId?.Value,
            amount = l.Amount,
            currency = l.Currency,
            status = l.Status.ToString(),
            externalRef = l.ExternalRef,
        }),
        openedAt = s.OpenedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = s.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
