using DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1;
using NotificationGrpc = DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1.NotificationManagementService;

namespace DKH.McpGateway.Application.Tools.Notifications;

[McpServerToolType]
public static class GetNotificationStatusTool
{
    [McpServerTool(Name = "get_notification_status"), Description(
        "Get delivery status by order ID. Read-only; recipient email, phone or chat IDs are masked from the response.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        NotificationGrpc.NotificationManagementServiceClient client,
        [Description("Order ID (GUID)")] string orderId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(orderId))
        {
            return McpProtoHelper.FormatError("orderId is required");
        }

        var response = await client.GetNotificationStatusAsync(
            new GetNotificationStatusRequest { OrderId = new GuidValue(orderId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(NotificationMapper.MapDelivery(response), McpJsonDefaults.Options);
    }
}
