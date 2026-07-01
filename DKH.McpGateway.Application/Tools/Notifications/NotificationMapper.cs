using DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1;
using DKH.NotificationService.Contracts.Notification.Models.NotificationDelivery.v1;
using DKH.NotificationService.Contracts.Notification.Models.NotificationHealth.v1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Notifications;

internal static class NotificationMapper
{
    internal static object MapHealth(NotificationHealthResponse response) => new
    {
        statuses = MapHealthStatus(response.Statuses),
        channels = response.Channels.Select(MapChannelStats),
    };

    internal static object MapDelivery(NotificationDeliveryModel model) => new
    {
        id = model.Id?.Value,
        orderId = model.OrderId?.Value,
        channel = model.Channel.ToString(),
        status = model.Status.ToString(),
        message = model.Message,
        createdAtUtc = FormatTimestamp(model.CreatedAtUtc),
        deliveredAtUtc = FormatTimestamp(model.DeliveredAtUtc),
        failureReason = model.FailureReason,
        botId = model.BotId?.Value,
        parseMode = model.ParseMode,
        disableNotification = model.HasDisableNotification ? model.DisableNotification : (bool?)null,
        storefrontId = model.StorefrontId?.Value,
        // recipient omitted: email/phone/chat id is contact PII.
    };

    internal static object MapBulkJobStatus(GetBulkJobStatusResponse response) => new
    {
        jobId = response.JobId?.Value,
        status = response.Status.ToString(),
        totalRecipients = response.TotalRecipients,
        processedCount = response.ProcessedCount,
        sentCount = response.SentCount,
        failedCount = response.FailedCount,
        createdAt = FormatTimestamp(response.CreatedAt),
        startedAt = FormatTimestamp(response.StartedAt),
        completedAt = FormatTimestamp(response.CompletedAt),
        failureReason = response.HasFailureReason ? response.FailureReason : null,
    };

    internal static object MapBulkJobSummary(BulkJobSummaryModel model) => new
    {
        jobId = model.JobId?.Value,
        idempotencyKey = model.IdempotencyKey,
        channel = model.Channel.ToString(),
        status = model.Status.ToString(),
        totalRecipients = model.TotalRecipients,
        sentCount = model.SentCount,
        failedCount = model.FailedCount,
        createdAt = FormatTimestamp(model.CreatedAt),
        storefrontId = model.StorefrontId?.Value,
    };

    internal static object MapBulkRecipientFailure(BulkRecipientFailureModel model) => new
    {
        recipientId = model.RecipientId?.Value,
        failureReason = model.FailureReason,
        attemptCount = model.AttemptCount,
        processedAt = FormatTimestamp(model.ProcessedAt),
        // recipient omitted: email/phone/chat id is contact PII.
    };

    internal static object MapPagination(PaginationMetadata? metadata, int fallbackPage, int fallbackPageSize) => new
    {
        totalCount = metadata?.TotalCount ?? 0,
        currentPage = metadata?.CurrentPage ?? fallbackPage,
        pageSize = metadata?.PageSize ?? fallbackPageSize,
        totalPages = metadata?.TotalPages ?? 0,
        hasNextPage = metadata?.HasNextPage ?? false,
        hasPreviousPage = metadata?.HasPreviousPage ?? false,
    };

    private static object MapHealthStatus(NotificationHealthStatusSummaryModel? model) => new
    {
        total = model?.Total ?? 0,
        pending = model?.Pending ?? 0,
        sent = model?.Sent ?? 0,
        failed = model?.Failed ?? 0,
    };

    private static object MapChannelStats(NotificationChannelStatsModel model) => new
    {
        channel = model.Channel.ToString(),
        total = model.Total,
        pending = model.Pending,
        sent = model.Sent,
        failed = model.Failed,
    };

    private static string? FormatTimestamp(Timestamp? timestamp)
        => timestamp?.ToDateTimeOffset().ToString("O");
}
