using DKH.ReferenceService.Contracts.Api.DeliveryQuery.V1;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tool for listing delivery time options.
/// </summary>
[McpServerToolType]
public static class ListDeliveryTimesTool
{
    [McpServerTool(Name = "list_delivery_times"), Description("List available delivery time options with estimated day ranges.")]
    public static async Task<string> ExecuteAsync(
        DeliveryQueryService.DeliveryQueryServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetDeliveryTimesAsync(
            new GetDeliveryTimesRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken);

        var result = new
        {
            deliveryTimes = response.DeliveryTimes.Select(static d => new
            {
                code = d.Code,
                name = d.Name,
                color = d.Color,
                minDays = d.MinDays,
                maxDays = d.MaxDays,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
