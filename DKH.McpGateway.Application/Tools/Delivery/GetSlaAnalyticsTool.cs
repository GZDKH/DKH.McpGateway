using DKH.DeliveryService.Contracts.Delivery.Api.Analytics.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class GetSlaAnalyticsTool
{
    [McpServerTool(Name = "get_sla_analytics"), Description(
        "Get SLA analytics for a delivery partner. " +
        "Filter by period, lane, and date range. " +
        "Returns on-time counts, delay counts, average delay hours, and ETA accuracy.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AnalyticsService.AnalyticsServiceClient client,
        [Description("Partner counterparty ID (GUID)")] string partnerId,
        [Description("SLA period (e.g. Day, Week, Month)")] string period,
        [Description("Lane string filter (optional)")] string? lane = null,
        [Description("Range start (ISO 8601, optional)")] string? fromUtc = null,
        [Description("Range end (ISO 8601, optional)")] string? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(partnerId))
        {
            return McpProtoHelper.FormatError("partnerId is required");
        }

        if (string.IsNullOrWhiteSpace(period))
        {
            return McpProtoHelper.FormatError("period is required");
        }

        var request = new GetSlaAnalyticsRequest
        {
            PartnerId = new GuidValue(partnerId),
            Period = Enum.Parse<SlaPeriod>(period, ignoreCase: true),
        };

        if (!string.IsNullOrWhiteSpace(lane))
        {
            request.Lane = lane;
        }

        if (!string.IsNullOrWhiteSpace(fromUtc) && DateTime.TryParse(fromUtc, out var fromDate))
        {
            request.FromUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(fromDate.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(toUtc) && DateTime.TryParse(toUtc, out var toDate))
        {
            request.ToUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(toDate.ToUniversalTime());
        }

        var response = await client.GetSlaAnalyticsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(s => new
            {
                partnerId = s.PartnerId?.Value,
                lane = s.Lane,
                period = s.Period.ToString(),
                periodStart = s.PeriodStart?.ToDateTimeOffset().ToString("O"),
                onTimeCount = s.OnTimeCount,
                delayedCount = s.DelayedCount,
                exceptionCount = s.ExceptionCount,
                avgDelayHours = s.AvgDelayHours,
                etaAccuracy = s.EtaAccuracy,
                updatedAt = s.UpdatedAt?.ToDateTimeOffset().ToString("O"),
            }),
        }, McpJsonDefaults.Options);
    }
}
