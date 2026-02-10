using DKH.TelegramBotService.Contracts.Management.V1;

namespace DKH.McpGateway.Application.Tools.Telegram;

[McpServerToolType]
public static class ManageTelegramChannelsTool
{
    [McpServerTool(Name = "manage_telegram_channels"), Description(
        "Manage Telegram channels linked to a bot. " +
        "Actions: 'list' to view channels, 'add' to link a channel, " +
        "'remove' to unlink, 'update_stats' to sync subscriber count, 'broadcast' to send a message.")]
    public static async Task<string> ExecuteAsync(
        TelegramBotManagement.TelegramBotManagementClient client,
        [Description("Bot ID (required for all actions)")] string botId,
        [Description("Action: list, add, remove, update_stats, or broadcast")] string action,
        [Description("Telegram channel ID (numeric or @handle, for add)")] string? telegramChannelId = null,
        [Description("Channel display name (for add)")] string? channelName = null,
        [Description("Channel ID returned from list (for remove/update_stats/broadcast)")] string? channelId = null,
        [Description("Message text (for broadcast)")] string? message = null,
        [Description("Parse mode: HTML, Markdown, or MarkdownV2 (for broadcast)")] string? parseMode = null,
        [Description("Send silently without notification (for broadcast)")] bool? disableNotification = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(action, "list", StringComparison.OrdinalIgnoreCase))
        {
            var response = await client.GetChannelsAsync(
                new GetChannelsRequest { BotId = botId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                channels = response.Channels.Select(c => new
                {
                    c.ChannelId,
                    c.BotId,
                    c.TelegramChannelId,
                    c.Name,
                    c.SubscribersCount,
                    c.IsVerified,
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "add", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(telegramChannelId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "telegramChannelId is required for add (numeric ID or @handle)" },
                    McpJsonDefaults.Options);
            }

            var response = await client.AddChannelAsync(
                new AddChannelRequest
                {
                    BotId = botId,
                    TelegramChannelId = telegramChannelId,
                    Name = channelName ?? "",
                },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "added",
                channel = new
                {
                    response.ChannelId,
                    response.TelegramChannelId,
                    response.Name,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(channelId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "channelId is required for remove (get it from 'list' action)" },
                    McpJsonDefaults.Options);
            }

            var response = await client.RemoveChannelAsync(
                new RemoveChannelRequest { ChannelId = channelId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "removed",
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update_stats", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(channelId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "channelId is required for update_stats" },
                    McpJsonDefaults.Options);
            }

            var response = await client.UpdateChannelStatsAsync(
                new UpdateChannelStatsRequest { ChannelId = channelId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "stats_updated",
                subscribersCount = response.SubscribersCount,
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "broadcast", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(message))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "channelId and message are required for broadcast" },
                    McpJsonDefaults.Options);
            }

            var request = new BroadcastToChannelRequest
            {
                BotId = botId,
                ChannelId = channelId,
                Message = message,
                DisableNotification = disableNotification ?? false,
            };

            if (!string.IsNullOrEmpty(parseMode))
            {
                request.ParseMode = parseMode;
            }

            var response = await client.BroadcastToChannelAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "broadcast_sent",
                messageId = response.MessageId,
                errorMessage = response.ErrorMessage,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: list, add, remove, update_stats, or broadcast" },
            McpJsonDefaults.Options);
    }
}
