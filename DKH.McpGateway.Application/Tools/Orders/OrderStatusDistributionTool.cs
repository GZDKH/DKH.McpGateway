using DKH.OrderService.Contracts.Models.V1;
using GrpcOrderService = DKH.OrderService.Contracts.Api.V1.OrderService;

namespace DKH.McpGateway.Application.Tools.Orders;

[McpServerToolType]
public static class OrderStatusDistributionTool
{
    [McpServerTool(Name = "order_status_distribution"), Description("Get order status breakdown with counts, percentages, and conversion rate.")]
    public static async Task<string> ExecuteAsync(
        GrpcOrderService.OrderServiceClient client,
        [Description("Start of period (ISO 8601, e.g. 2024-01-01)")] string? periodStart = null,
        [Description("End of period (ISO 8601, e.g. 2024-12-31)")] string? periodEnd = null,
        [Description("Storefront ID (UUID) to scope the analysis")] string? storefrontId = null,
        CancellationToken cancellationToken = default)
    {
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

        var statusBreakdown = orders
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                status = g.Key switch
                {
                    OrderStatus.Pending => "pending",
                    OrderStatus.Confirmed => "confirmed",
                    OrderStatus.Completed => "completed",
                    OrderStatus.Cancelled => "cancelled",
                    _ => "unknown",
                },
                count = g.Count(),
                percentage = Math.Round(100.0 * g.Count() / orders.Count, 1),
            })
            .OrderByDescending(s => s.count)
            .ToList();

        var completed = orders.Count(o => o.Status == OrderStatus.Completed);
        var cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled);

        var result = new
        {
            totalOrders = totalCount,
            fetchedOrders = orders.Count,
            statusBreakdown,
            conversionRate = Math.Round(100.0 * completed / orders.Count, 1),
            cancellationRate = Math.Round(100.0 * cancelled / orders.Count, 1),
            periodStart,
            periodEnd,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
