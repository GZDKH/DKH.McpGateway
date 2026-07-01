using DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1;
using NotificationGrpc = DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1.NotificationManagementService;

namespace DKH.McpGateway.Application.Tools.Notifications;

[McpServerToolType]
public static class GetNotificationHealthTool
{
    [McpServerTool(Name = "get_notification_health"), Description(
        "Get NotificationService delivery health counters by status and channel. Read-only; no recipient PII is returned.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NotificationGrpc.NotificationManagementServiceClient client,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.GetNotificationHealthAsync(
            new GetNotificationHealthRequest(),
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(NotificationMapper.MapHealth(response), McpJsonDefaults.Options);
    }
}
