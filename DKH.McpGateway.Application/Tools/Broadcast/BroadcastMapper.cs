using DKH.BroadcastService.Contracts.Broadcast.Models.v1;

namespace DKH.McpGateway.Application.Tools.Broadcast;

internal static class BroadcastMapper
{
    // BroadcastModel has no contact PII; target config is audience/channel configuration, not personal contact data.
    internal static object MapBroadcast(BroadcastModel m) => new
    {
        id = m.Id?.Value,
        storefrontId = m.StorefrontId?.Value,
        targetType = m.TargetType,
        targetConfig = m.TargetConfig,
        text = m.Text,
        parseMode = m.ParseMode,
        scheduledAt = m.ScheduledAt?.ToDateTimeOffset().ToString("O"),
        timeZoneId = m.TimeZoneId,
        status = m.Status.ToString(),
        executedAt = m.ExecutedAt?.ToDateTimeOffset().ToString("O"),
        errorMessage = m.ErrorMessage,
        externalMessageId = m.ExternalMessageId,
        recurrence = m.Recurrence.ToString(),
        cronExpression = m.CronExpression,
        recurrenceEndAt = m.RecurrenceEndAt?.ToDateTimeOffset().ToString("O"),
        parentBroadcastId = m.ParentBroadcastId?.Value,
        retryCount = m.RetryCount,
        createdAt = m.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = m.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
