using DKH.OrderService.Contracts.Api.V1;
using DKH.OrderService.Contracts.Models.V1;
using Google.Protobuf.WellKnownTypes;
using GrpcOrderService = DKH.OrderService.Contracts.Api.V1.OrderService;

namespace DKH.McpGateway.Application.Tools.Orders;

internal static class OrderQueryHelper
{
    internal const int MaxPages = 20;
    internal const int PageSize = 50;
    internal const int KAnonymityThreshold = 5;

    internal static (DateTimeOffset? From, DateTimeOffset? To, string? Error) ParseDateRange(
        string? periodStart, string? periodEnd)
    {
        DateTimeOffset? from = null, to = null;

        if (!string.IsNullOrEmpty(periodStart))
        {
            if (!DateTimeOffset.TryParse(periodStart, out var f))
            {
                return (null, null, $"Invalid periodStart format: {periodStart}");
            }

            from = f;
        }

        if (!string.IsNullOrEmpty(periodEnd))
        {
            if (!DateTimeOffset.TryParse(periodEnd, out var t))
            {
                return (null, null, $"Invalid periodEnd format: {periodEnd}");
            }

            to = t;
        }

        if (from.HasValue && to.HasValue)
        {
            if (to < from)
            {
                return (null, null, "periodEnd must be after periodStart");
            }

            if ((to.Value - from.Value).TotalDays > 365)
            {
                return (null, null, "Date range cannot exceed 1 year");
            }
        }

        return (from, to, null);
    }

    internal static async Task<(List<Order> Orders, int TotalCount, string? Error)> FetchOrdersAsync(
        GrpcOrderService.OrderServiceClient client,
        string? storefrontId,
        string? periodStart,
        string? periodEnd,
        CancellationToken ct)
    {
        var (from, to, error) = ParseDateRange(periodStart, periodEnd);
        if (error is not null)
        {
            return ([], 0, error);
        }

        var orders = new List<Order>();
        var totalCount = 0;
        var page = 1;

        while (page <= MaxPages)
        {
            var request = new ListOrdersRequest
            {
                StorefrontId = storefrontId ?? string.Empty,
                Page = page,
                PageSize = PageSize,
            };

            if (from.HasValue)
            {
                request.From = Timestamp.FromDateTimeOffset(from.Value);
            }

            if (to.HasValue)
            {
                request.To = Timestamp.FromDateTimeOffset(to.Value);
            }

            var response = await client.ListOrdersAsync(request, cancellationToken: ct);
            totalCount = response.Total;
            orders.AddRange(response.Items);

            if (orders.Count >= totalCount || response.Items.Count < PageSize)
            {
                break;
            }

            page++;
        }

        return (orders, totalCount, null);
    }
}
