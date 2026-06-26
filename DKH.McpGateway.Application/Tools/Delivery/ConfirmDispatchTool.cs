using DKH.DeliveryService.Contracts.Delivery.Api.Dispatch.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class ConfirmDispatchTool
{
    [McpServerTool(Name = "confirm_dispatch"), Description(
        "Confirm dispatch of a fulfillment leg. " +
        "Attaches document references and returns the dispatch manifest.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DispatchService.DispatchServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("Leg ID (GUID)")] string legId,
        [Description("Comma-separated document reference strings")] string documentRefs,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(legId))
        {
            return McpProtoHelper.FormatError("legId is required");
        }

        if (string.IsNullOrWhiteSpace(documentRefs))
        {
            return McpProtoHelper.FormatError("documentRefs is required");
        }

        var request = new ConfirmDispatchRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
            LegId = new GuidValue(legId),
        };

        request.DocumentRefs.AddRange(documentRefs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var manifest = await client.ConfirmDispatchAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapDispatchManifest(manifest), McpJsonDefaults.Options);
    }

    internal static object MapDispatchManifest(DispatchManifestModel m) => new
    {
        id = m.Id?.Value,
        fulfillmentId = m.FulfillmentId?.Value,
        firstLegId = m.FirstLegId?.Value,
        handlingUnitIds = m.HandlingUnitIds.Select(g => g.Value),
        documentRefs = m.DocumentRefs,
        dispatchedAt = m.DispatchedAt?.ToDateTimeOffset().ToString("O"),
    };
}
