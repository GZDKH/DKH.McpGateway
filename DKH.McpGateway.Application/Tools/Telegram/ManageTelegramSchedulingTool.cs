using DKH.TelegramBotService.Contracts.Scheduling.V1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Telegram;

[McpServerToolType]
public static class ManageTelegramSchedulingTool
{
    [McpServerTool(Name = "manage_telegram_scheduling"), Description(
        "Schedule and manage Telegram broadcast messages. " +
        "Actions: 'create' to schedule a new message, 'list' to view scheduled messages, " +
        "'update' to modify pending message, 'cancel' to cancel a pending message.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TelegramScheduling.TelegramSchedulingClient client,
        [Description("Bot ID (required for all actions)")] string botId,
        [Description("Action: create, list, update, or cancel")] string action,
        [Description("Target chat/channel ID (for create)")] string? chatId = null,
        [Description("Message text (for create/update)")] string? text = null,
        [Description("Scheduled time in ISO 8601 format, e.g. '2025-01-15T10:00:00Z' (for create/update)")] string? scheduledAt = null,
        [Description("Parse mode: HTML, Markdown, or MarkdownV2 (for create/update)")] string? parseMode = null,
        [Description("IANA timezone ID, e.g. 'Europe/Moscow' (for create/update)")] string? timeZoneId = null,
        [Description("Broadcast ID (for update/cancel, returned from list/create)")] string? broadcastId = null,
        [Description("Filter by status: pending, executed, cancelled, failed (for list)")] string? status = null,
        [Description("Page number, 1-based (for list)")] int? page = null,
        [Description("Page size, 1-100 (for list)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(chatId) || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(scheduledAt))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "chatId, text, and scheduledAt are required for create" },
                    McpJsonDefaults.Options);
            }

            if (!DateTimeOffset.TryParse(scheduledAt, out var scheduledTime))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = $"Invalid scheduledAt format: '{scheduledAt}'. Use ISO 8601, e.g. '2025-01-15T10:00:00Z'" },
                    McpJsonDefaults.Options);
            }

            var request = new CreateScheduledBroadcastRequest
            {
                BotId = botId,
                ChatId = chatId,
                Text = text,
                ScheduledAt = Timestamp.FromDateTimeOffset(scheduledTime),
                TimeZoneId = timeZoneId ?? "UTC",
            };

            if (!string.IsNullOrEmpty(parseMode))
            {
                request.ParseMode = parseMode;
            }

            var response = await client.CreateScheduledBroadcastAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "created",
                broadcast = new
                {
                    response.Id,
                    response.BotId,
                    response.ChatId,
                    scheduledAt = response.ScheduledAt?.ToDateTimeOffset().ToString("O"),
                    response.TimeZoneId,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "list", StringComparison.OrdinalIgnoreCase))
        {
            var request = new ListScheduledBroadcastsRequest
            {
                BotId = botId,
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            };

            if (!string.IsNullOrEmpty(status))
            {
                request.Status = ParseStatus(status);
            }

            var response = await client.ListScheduledBroadcastsAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                totalCount = response.TotalCount,
                page = response.Page,
                pageSize = response.PageSize,
                items = response.Items.Select(i => new
                {
                    i.Id,
                    i.BotId,
                    i.ChatId,
                    i.Text,
                    scheduledAt = i.ScheduledAt?.ToDateTimeOffset().ToString("O"),
                    i.TimeZoneId,
                    status = i.Status.ToString(),
                    executedAt = i.ExecutedAt?.ToDateTimeOffset().ToString("O"),
                    errorMessage = i.ErrorMessage,
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(broadcastId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "broadcastId is required for update" },
                    McpJsonDefaults.Options);
            }

            var request = new UpdateScheduledBroadcastRequest
            {
                Id = broadcastId,
            };

            if (!string.IsNullOrEmpty(text))
            {
                request.Text = text;
            }

            if (!string.IsNullOrEmpty(scheduledAt) && DateTimeOffset.TryParse(scheduledAt, out var newTime))
            {
                request.ScheduledAt = Timestamp.FromDateTimeOffset(newTime);
            }

            if (!string.IsNullOrEmpty(parseMode))
            {
                request.ParseMode = parseMode;
            }

            if (!string.IsNullOrEmpty(timeZoneId))
            {
                request.TimeZoneId = timeZoneId;
            }

            var response = await client.UpdateScheduledBroadcastAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                broadcast = new
                {
                    response.Id,
                    response.Text,
                    scheduledAt = response.ScheduledAt?.ToDateTimeOffset().ToString("O"),
                    response.TimeZoneId,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "cancel", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(broadcastId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "broadcastId is required for cancel" },
                    McpJsonDefaults.Options);
            }

            var response = await client.CancelScheduledBroadcastAsync(
                new CancelScheduledBroadcastRequest { Id = broadcastId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "cancelled",
                broadcastId,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, list, update, or cancel" },
            McpJsonDefaults.Options);
    }

    private static ScheduledBroadcastStatus ParseStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "PENDING" => ScheduledBroadcastStatus.Pending,
        "EXECUTED" => ScheduledBroadcastStatus.Executed,
        "CANCELLED" => ScheduledBroadcastStatus.Cancelled,
        "FAILED" => ScheduledBroadcastStatus.Failed,
        _ => ScheduledBroadcastStatus.Unspecified,
    };
}
