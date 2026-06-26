using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class ListRateCardsTool
{
    [McpServerTool(Name = "list_rate_cards"), Description(
        "List rate cards with pagination. " +
        "Returns a paged list of rate cards including carrier, service level, currency, validity and status.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        RateCardService.RateCardServiceClient client,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListRateCardsAsync(
            new ListRateCardsRequest { Page = page, PageSize = pageSize },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetRateCardTool.MapRateCard),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}
