using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class ListSurchargeRulesTool
{
    [McpServerTool(Name = "list_surcharge_rules"), Description(
        "List surcharge rules with optional carrier filter and active-only flag. " +
        "Returns a paged list of surcharge rules.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SurchargeRuleService.SurchargeRuleServiceClient client,
        [Description("Filter by carrier code (empty = all carriers)")] string? carrier = null,
        [Description("Return only active rules")] bool activeOnly = false,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListSurchargeRulesAsync(
            new ListSurchargeRulesRequest
            {
                Carrier = carrier ?? string.Empty,
                ActiveOnly = activeOnly,
                Page = page,
                PageSize = pageSize,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetSurchargeRuleTool.MapSurchargeRule),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}
