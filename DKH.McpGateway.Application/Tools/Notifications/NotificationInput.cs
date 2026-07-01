using DKH.NotificationService.Contracts.Notification.Models.BulkJobStatus.v1;

namespace DKH.McpGateway.Application.Tools.Notifications;

internal static class NotificationInput
{
    internal static BulkJobStatus ParseBulkJobStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BulkJobStatus.Unspecified;
        }

        return Enum.TryParse<BulkJobStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : BulkJobStatus.Unspecified;
    }
}
