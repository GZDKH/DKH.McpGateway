using DKH.CounterpartyService.Contracts.Counterparty.Services.v1;

namespace DKH.McpGateway.Application.Tools.Counterparty;

[McpServerToolType]
public static class GetCounterpartyBalanceTool
{
    [McpServerTool(Name = "get_counterparty_balance"), Description("Get accounts-payable balance rows for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyApBalanceService.CounterpartyApBalanceServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("ISO-4217 currency filter (optional)")] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.GetCounterpartyBalanceAsync(
            new GetCounterpartyBalanceRequest
            {
                CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
                Currency = CounterpartyToolInput.OptionalString(currency),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(new { balances = response.Balances.Select(CounterpartyMapper.MapBalance) });
    }
}

[McpServerToolType]
public static class GetCounterpartyFinancialDashboardTool
{
    [McpServerTool(Name = "get_counterparty_financial_dashboard"), Description("Get the financial dashboard projection for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CounterpartyFinancialDashboardService.CounterpartyFinancialDashboardServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("Recent reconciliation act limit")] int reconciliationLimit = 10,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.GetCounterpartyFinancialDashboardAsync(
            new GetCounterpartyFinancialDashboardRequest
            {
                CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
                ReconciliationLimit = Math.Max(reconciliationLimit, 0),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapFinancialDashboard(response));
    }
}
