using DKH.ReferenceService.Contracts.Reference.Api.DeliveryTimeManagement.v1;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tool for listing delivery time options.
/// </summary>
[McpServerToolType]
public static class ListDeliveryTimesTool
{
    [McpServerTool(Name = "list_delivery_times"), Description("List available delivery time options with estimated day ranges.")]
    public static async Task<string> ExecuteAsync(
        DeliveryTimeManagementService.DeliveryTimeManagementServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var response = await client.ListAsync(
            new ListDeliveryTimesRequest { Language = languageCode, PageSize = 1000 },
            cancellationToken: cancellationToken);

        var result = new
        {
            deliveryTimes = response.Items.Select(static d => new
            {
                code = d.Code,
                name = d.Translations.FirstOrDefault()?.Name ?? string.Empty,
                color = d.Color,
                minDays = d.DeliveryNotBeforeDays,
                maxDays = d.DeliveryNotLateDays,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
