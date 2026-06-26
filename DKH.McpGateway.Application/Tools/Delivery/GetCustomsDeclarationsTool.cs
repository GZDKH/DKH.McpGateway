using DKH.DeliveryService.Contracts.Delivery.Api.CustomsLink.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class GetCustomsDeclarationsTool
{
    [McpServerTool(Name = "get_customs_declarations"), Description(
        "Get all customs declaration IDs attached to a fulfillment.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CustomsLinkService.CustomsLinkServiceClient client,
        [Description("Fulfillment ID (GUID)")] string fulfillmentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(fulfillmentId))
        {
            return McpProtoHelper.FormatError("fulfillmentId is required");
        }

        var response = await client.GetDeclarationsAsync(
            new GetDeclarationsRequest { FulfillmentId = new GuidValue(fulfillmentId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            declarationIds = response.DeclarationIds.Select(g => g.Value),
        }, McpJsonDefaults.Options);
    }
}
