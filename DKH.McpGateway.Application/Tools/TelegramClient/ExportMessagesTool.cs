using DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1;
using MessageCollectionGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1.MessageCollectionService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class ExportMessagesTool
{
    [McpServerTool(Name = "export_messages"), Description("Export archived Telegram messages as base64 content.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        MessageCollectionGrpc.MessageCollectionServiceClient client,
        [Description("Monitored chat ID")] string chatId,
        [Description("Export format, for example json or csv")] string? format = "json",
        [Description("From timestamp, ISO-8601 (optional)")] string? from = null,
        [Description("To timestamp, ISO-8601 (optional)")] string? to = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (!TelegramClientToolInput.Required(chatId, nameof(chatId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var fromTimestamp = TelegramClientToolInput.ParseTimestamp(from, nameof(from), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var toTimestamp = TelegramClientToolInput.ParseTimestamp(to, nameof(to), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var exportFormat = TelegramClientToolInput.OptionalString(format);
        var request = new ExportMessagesRequest
        {
            ChatId = TelegramClientToolInput.ToGuid(chatId),
            Format = exportFormat.Length == 0 ? "json" : exportFormat,
        };
        if (fromTimestamp is not null)
        {
            request.From = fromTimestamp;
        }

        if (toTimestamp is not null)
        {
            request.To = toTimestamp;
        }

        var response = await client.ExportMessagesAsync(request, cancellationToken: cancellationToken);
        return TelegramClientToolOutput.Json(TelegramClientMapper.MapExport(response));
    }
}
