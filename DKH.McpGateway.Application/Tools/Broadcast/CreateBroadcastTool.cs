using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Application.Tools.Broadcast;

[McpServerToolType]
public static class CreateBroadcastTool
{
    [McpServerTool(Name = "create_broadcast"), Description(
        "Create and schedule a broadcast. targetConfig is audience/channel configuration, not contact PII.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        BroadcastGrpc.BroadcastServiceClient client,
        [Description("Storefront ID (GUID)")] string storefrontId,
        [Description("Delivery channel target type, e.g. telegram")] string targetType,
        [Description("Audience/target configuration payload (JSON string) - segment/channel selection")] string targetConfig,
        [Description("Broadcast message body")] string text,
        [Description("Message parse mode (optional), e.g. HTML or MarkdownV2")] string? parseMode = null,
        [Description("When to send (ISO 8601)")] string? scheduledAt = null,
        [Description("IANA time zone id, e.g. Europe/London")] string timeZoneId = "UTC",
        [Description("Recurrence: None, Daily, Weekly, Monthly, Custom (default None)")] string? recurrence = null,
        [Description("Cron expression for Custom recurrence (optional)")] string? cronExpression = null,
        [Description("Recurrence end (ISO 8601, optional)")] string? recurrenceEndAt = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(storefrontId))
        {
            return McpProtoHelper.FormatError("storefrontId is required");
        }

        if (string.IsNullOrWhiteSpace(targetType))
        {
            return McpProtoHelper.FormatError("targetType is required");
        }

        if (string.IsNullOrWhiteSpace(targetConfig))
        {
            return McpProtoHelper.FormatError("targetConfig is required");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return McpProtoHelper.FormatError("text is required");
        }

        if (!BroadcastInput.TryParseTimestamp(scheduledAt, out var scheduledAtTimestamp))
        {
            return McpProtoHelper.FormatError("scheduledAt must be ISO 8601");
        }

        var request = new CreateBroadcastRequest
        {
            StorefrontId = new GuidValue(storefrontId),
            TargetType = targetType,
            TargetConfig = targetConfig,
            Text = text,
            ScheduledAt = scheduledAtTimestamp,
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId,
            Recurrence = BroadcastInput.ParseRecurrence(recurrence),
        };

        if (!string.IsNullOrWhiteSpace(parseMode))
        {
            request.ParseMode = parseMode;
        }

        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            request.CronExpression = cronExpression;
        }

        if (!string.IsNullOrWhiteSpace(recurrenceEndAt))
        {
            if (!BroadcastInput.TryParseTimestamp(recurrenceEndAt, out var recurrenceEndAtTimestamp))
            {
                return McpProtoHelper.FormatError("recurrenceEndAt must be ISO 8601");
            }

            request.RecurrenceEndAt = recurrenceEndAtTimestamp;
        }

        var model = await client.CreateBroadcastAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(BroadcastMapper.MapBroadcast(model), McpJsonDefaults.Options);
    }
}
