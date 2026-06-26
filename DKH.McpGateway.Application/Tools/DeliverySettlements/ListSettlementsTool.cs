using DKH.DeliveryService.Contracts.Delivery.Api.Settlements.v1;

namespace DKH.McpGateway.Application.Tools.DeliverySettlements;

[McpServerToolType]
public static class ListSettlementsTool
{
    [McpServerTool(Name = "list_settlements"), Description(
        "List settlement records with optional filtering by party and date range.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SettlementsService.SettlementsServiceClient client,
        [Description("Party ID (GUID, optional)")] string? partyId = null,
        [Description("Range start (ISO 8601, optional)")] string? fromUtc = null,
        [Description("Range end (ISO 8601, optional)")] string? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListSettlementsRequest();

        if (!string.IsNullOrWhiteSpace(partyId))
        {
            request.PartyId = new GuidValue(partyId);
        }

        if (!string.IsNullOrWhiteSpace(fromUtc) && DateTime.TryParse(fromUtc, out var fromDate))
        {
            request.FromUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(fromDate.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(toUtc) && DateTime.TryParse(toUtc, out var toDate))
        {
            request.ToUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(toDate.ToUniversalTime());
        }

        var response = await client.ListSettlementsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetSettlementTool.MapSettlement),
        }, McpJsonDefaults.Options);
    }
}
