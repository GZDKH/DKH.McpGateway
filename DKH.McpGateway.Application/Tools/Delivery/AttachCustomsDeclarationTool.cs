using DKH.DeliveryService.Contracts.Delivery.Api.CustomsLink.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class AttachCustomsDeclarationTool
{
    [McpServerTool(Name = "attach_customs_declaration"), Description(
        "Attach a customs declaration to a fulfillment. " +
        "Returns the updated fulfillment with the declaration ID appended (delivery address omitted).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CustomsLinkService.CustomsLinkServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        [Description("Customs declaration ID (GUID)")] string declarationId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        if (string.IsNullOrWhiteSpace(declarationId))
        {
            return McpProtoHelper.FormatError("declarationId is required");
        }

        var request = new AttachDeclarationRequest
        {
            FulfillmentId = new GuidValue(fulfillmentId),
            DeclarationId = new GuidValue(declarationId),
        };

        var fulfillment = await client.AttachDeclarationAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetFulfillmentTool.MapFulfillment(fulfillment), McpJsonDefaults.Options);
    }
}
