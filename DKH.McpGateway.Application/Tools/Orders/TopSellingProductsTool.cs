using GrpcOrderService = DKH.OrderService.Contracts.Api.V1.OrderService;

namespace DKH.McpGateway.Application.Tools.Orders;

[McpServerToolType]
public static class TopSellingProductsTool
{
    [McpServerTool(Name = "top_selling_products"), Description("Get best-selling products by quantity and revenue (aggregated, no customer data).")]
    public static async Task<string> ExecuteAsync(
        GrpcOrderService.OrderServiceClient client,
        [Description("Start of period (ISO 8601)")] string? periodStart = null,
        [Description("End of period (ISO 8601)")] string? periodEnd = null,
        [Description("Number of top products to return (default 10, max 50)")] int limit = 10,
        [Description("Storefront ID (UUID) to scope the analysis")] string? storefrontId = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

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

        var products = orders
            .SelectMany(o => o.Items.Select(i => new { OrderId = o.Id, Item = i }))
            .GroupBy(x => x.Item.ProductId)
            .Where(g => g.Select(x => x.OrderId).Distinct().Count() >= OrderQueryHelper.KAnonymityThreshold)
            .Select(g => new
            {
                productId = g.Key,
                name = g.First().Item.Name,
                sku = g.First().Item.Sku,
                totalQuantity = g.Sum(x => x.Item.Quantity),
                totalRevenue = Math.Round(g.Sum(x => x.Item.UnitPrice * x.Item.Quantity), 2),
            })
            .OrderByDescending(p => p.totalQuantity)
            .Take(limit)
            .ToList();

        var result = new
        {
            totalOrders = totalCount,
            fetchedOrders = orders.Count,
            products,
            periodStart,
            periodEnd,
            note = products.Count == 0
                ? $"No products met the k-anonymity threshold (>={OrderQueryHelper.KAnonymityThreshold} distinct orders)"
                : null,
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
