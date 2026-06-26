using DKH.DeliveryService.Contracts.Delivery.Api.DeliveryCrud.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class GetDeliveryQuoteTool
{
    [McpServerTool(Name = "get_delivery_quote"), Description(
        "Get a single delivery quote by its ID. " +
        "Returns the quote with rate snapshot, status, and timestamps.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DeliveryCrudService.DeliveryCrudServiceClient client,
        [Description("Quote ID (GUID)")] string quoteId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(quoteId))
        {
            return McpProtoHelper.FormatError("quoteId is required");
        }

        var quote = await client.GetQuoteAsync(
            new GetQuoteRequest { Id = new GuidValue(quoteId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(CreateDeliveryQuoteTool.MapQuote(quote), McpJsonDefaults.Options);
    }
}
