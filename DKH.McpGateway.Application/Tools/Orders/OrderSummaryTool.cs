using DKH.OrderService.Contracts.Order.Api.OrderCrud.v1;
using DKH.OrderService.Contracts.Order.Models.Order.v1;

namespace DKH.McpGateway.Application.Tools.Orders;

[McpServerToolType]
public static class OrderSummaryTool
{
    [McpServerTool(Name = "order_summary"), Description("Get aggregated order statistics: total count, revenue, average order value, breakdown by status.")]
    public static async Task<string> ExecuteAsync(
        OrderCrudService.OrderCrudServiceClient client,
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

        var totalRevenue = orders.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity));
        var avgOrderValue = totalRevenue / orders.Count;

        var statusCounts = orders
            .GroupBy(o => o.Status)
            .ToDictionary(
                g => g.Key switch
                {
                    OrderStatus.Pending => "pending",
                    OrderStatus.Confirmed => "confirmed",
                    OrderStatus.Completed => "completed",
                    OrderStatus.Cancelled => "cancelled",
                    _ => "unknown",
                },
                g => g.Count());

        var result = new
        {
            totalOrders = totalCount,
            fetchedOrders = orders.Count,
            totalRevenue = Math.Round(totalRevenue, 2),
            avgOrderValue = Math.Round(avgOrderValue, 2),
            ordersByStatus = statusCounts,
            periodStart,
            periodEnd,
            note = orders.Count < totalCount
                ? $"Analysis based on {orders.Count} of {totalCount} orders"
                : null,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
