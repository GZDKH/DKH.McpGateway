using System.Globalization;
using GrpcOrderService = DKH.OrderService.Contracts.Api.V1.OrderService;

namespace DKH.McpGateway.Application.Tools.Orders;

[McpServerToolType]
public static class OrderTrendsTool
{
    private static readonly string[] ValidGranularities = ["day", "week", "month"];

    [McpServerTool(Name = "order_trends"), Description("Analyze order trends over time with configurable granularity (day, week, month).")]
    public static async Task<string> ExecuteAsync(
        GrpcOrderService.OrderServiceClient client,
        [Description("Start of period (ISO 8601, required)")] string periodStart,
        [Description("End of period (ISO 8601, required)")] string periodEnd,
        [Description("Time granularity: day, week, or month (default: day)")] string granularity = "day",
        [Description("Storefront ID (UUID) to scope the analysis")] string? storefrontId = null,
        CancellationToken cancellationToken = default)
    {
        if (!ValidGranularities.Contains(granularity))
        {
            return JsonSerializer.Serialize(new { error = "granularity must be 'day', 'week', or 'month'" }, McpJsonDefaults.Options);
        }

        var (orders, totalCount, error) = await OrderQueryHelper.FetchOrdersAsync(
            client, storefrontId, periodStart, periodEnd, cancellationToken);

        if (error is not null)
        {
            return JsonSerializer.Serialize(new { error }, McpJsonDefaults.Options);
        }

        if (orders.Count == 0)
        {
            return JsonSerializer.Serialize(new { totalOrders = 0, message = "No orders found for the specified period" }, McpJsonDefaults.Options);
        }

        var allPeriods = orders
            .Where(o => o.CreatedAt is not null)
            .GroupBy(o => GetPeriodKey(o.CreatedAt.ToDateTimeOffset(), granularity))
            .Select(g =>
            {
                var revenue = g.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity));
                return new
                {
                    period = g.Key,
                    orderCount = g.Count(),
                    revenue = Math.Round(revenue, 2),
                    avgOrderValue = Math.Round(revenue / g.Count(), 2),
                };
            })
            .OrderBy(g => g.period)
            .ToList();

        var visible = allPeriods
            .Where(g => g.orderCount >= OrderQueryHelper.KAnonymityThreshold)
            .ToList();

        var suppressed = allPeriods.Count - visible.Count;

        var result = new
        {
            totalOrders = totalCount,
            fetchedOrders = orders.Count,
            granularity,
            periods = visible,
            suppressedPeriods = suppressed > 0 ? suppressed : (int?)null,
            suppressionNote = suppressed > 0
                ? $"{suppressed} period(s) suppressed due to k-anonymity threshold (<{OrderQueryHelper.KAnonymityThreshold} orders)"
                : null,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }

    private static string GetPeriodKey(DateTimeOffset date, string granularity) => granularity switch
    {
        "week" => $"{ISOWeek.GetYear(date.DateTime):D4}-W{ISOWeek.GetWeekOfYear(date.DateTime):D2}",
        "month" => date.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        _ => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    };
}
