using DKH.TelegramBotService.Contracts.Management.V1;

namespace DKH.McpGateway.Application.Tools.Telegram;

[McpServerToolType]
public static class ManageTelegramManagerGroupsTool
{
    [McpServerTool(Name = "manage_telegram_manager_groups"), Description(
        "Manage Telegram manager groups for a bot. Manager groups receive notifications " +
        "about events like new orders, payments, etc. " +
        "Actions: 'list' to view groups, 'add' to create, 'remove' to delete.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TelegramBotManagement.TelegramBotManagementClient client,
        [Description("Bot ID (required for all actions)")] string botId,
        [Description("Action: list, add, or remove")] string action,
        [Description("Telegram group ID (for add)")] string? telegramGroupId = null,
        [Description("Group display name (for add)")] string? groupName = null,
        [Description("Notification types to subscribe: order_created, payment_received, etc. (comma-separated, for add)")] string? notificationTypes = null,
        [Description("Manager group ID returned from list (for remove)")] string? managerGroupId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "list", StringComparison.OrdinalIgnoreCase))
        {
            var response = await client.GetManagerGroupsAsync(
                new GetManagerGroupsRequest { BotId = new GuidValue(botId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                managerGroups = response.ManagerGroups.Select(g => new
                {
                    g.ManagerGroupId,
                    g.BotId,
                    g.TelegramGroupId,
                    g.Name,
                    notificationTypes = g.NotificationTypes.ToList(),
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "add", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(telegramGroupId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "telegramGroupId is required for add" },
                    McpJsonDefaults.Options);
            }

            var request = new AddManagerGroupRequest
            {
                BotId = new GuidValue(botId),
                TelegramGroupId = telegramGroupId,
                Name = groupName ?? "",
            };

            if (!string.IsNullOrEmpty(notificationTypes))
            {
                var types = notificationTypes
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                request.NotificationTypes.AddRange(types);
            }

            var response = await client.AddManagerGroupAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "added",
                managerGroup = new
                {
                    response.ManagerGroupId,
                    response.TelegramGroupId,
                    response.Name,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(managerGroupId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "managerGroupId is required for remove (get it from 'list' action)" },
                    McpJsonDefaults.Options);
            }

            var response = await client.RemoveManagerGroupAsync(
                new RemoveManagerGroupRequest { ManagerGroupId = new GuidValue(managerGroupId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "removed",
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: list, add, or remove" },
            McpJsonDefaults.Options);
    }
}
