using DKH.DeliveryService.Contracts.Delivery.Api.Cancellation.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class CancelFulfillmentTool
{
    [McpServerTool(Name = "cancel_fulfillment"), Description(
        "Cancel a fulfillment with a reason. " +
        "Returns the updated fulfillment with Cancelled status (delivery address omitted).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CancellationService.CancellationServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("Cancellation reason")] string reason,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return McpProtoHelper.FormatError("reason is required");
        }

        var request = new CancelFulfillmentRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
            Reason = reason,
        };

        var fulfillment = await client.CancelFulfillmentAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetFulfillmentTool.MapFulfillment(fulfillment), McpJsonDefaults.Options);
    }
}
