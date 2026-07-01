using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Application.Tools.Broadcast;

[McpServerToolType]
public static class UpdateBroadcastTool
{
    [McpServerTool(Name = "update_broadcast"), Description("Update broadcast content or schedule fields.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        BroadcastGrpc.BroadcastServiceClient client,
        [Description("Broadcast ID (GUID)")] string broadcastId,
        [Description("Broadcast message body (optional)")] string? text = null,
        [Description("Message parse mode (optional), e.g. HTML or MarkdownV2")] string? parseMode = null,
        [Description("When to send (ISO 8601, optional)")] string? scheduledAt = null,
        [Description("IANA time zone id, e.g. Europe/London (optional)")] string? timeZoneId = null,
        [Description("Recurrence: None, Daily, Weekly, Monthly, Custom (optional)")] string? recurrence = null,
        [Description("Cron expression for Custom recurrence (optional)")] string? cronExpression = null,
        [Description("Recurrence end (ISO 8601, optional)")] string? recurrenceEndAt = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(broadcastId))
        {
            return McpProtoHelper.FormatError("broadcastId is required");
        }

        var request = new UpdateBroadcastRequest { Id = new GuidValue(broadcastId) };

        if (!string.IsNullOrWhiteSpace(text))
        {
            request.Text = text;
        }

        if (!string.IsNullOrWhiteSpace(parseMode))
        {
            request.ParseMode = parseMode;
        }

        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            request.TimeZoneId = timeZoneId;
        }

        if (!string.IsNullOrWhiteSpace(recurrence))
        {
            request.Recurrence = BroadcastInput.ParseRecurrence(recurrence);
        }

        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            request.CronExpression = cronExpression;
        }

        if (!string.IsNullOrWhiteSpace(scheduledAt))
        {
            if (!BroadcastInput.TryParseTimestamp(scheduledAt, out var scheduledAtTimestamp))
            {
                return McpProtoHelper.FormatError("scheduledAt must be ISO 8601");
            }

            request.ScheduledAt = scheduledAtTimestamp;
        }

        if (!string.IsNullOrWhiteSpace(recurrenceEndAt))
        {
            if (!BroadcastInput.TryParseTimestamp(recurrenceEndAt, out var recurrenceEndAtTimestamp))
            {
                return McpProtoHelper.FormatError("recurrenceEndAt must be ISO 8601");
            }

            request.RecurrenceEndAt = recurrenceEndAtTimestamp;
        }

        var model = await client.UpdateBroadcastAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(BroadcastMapper.MapBroadcast(model), McpJsonDefaults.Options);
    }
}
