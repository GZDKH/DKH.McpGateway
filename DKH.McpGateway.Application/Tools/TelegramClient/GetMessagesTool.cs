using DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1;
using MessageCollectionGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1.MessageCollectionService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class GetMessagesTool
{
    [McpServerTool(Name = "get_messages"), Description("Get archived Telegram messages for a monitored chat.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        MessageCollectionGrpc.MessageCollectionServiceClient client,
        [Description("Monitored chat ID")] string chatId,
        [Description("From timestamp, ISO-8601 (optional)")] string? from = null,
        [Description("To timestamp, ISO-8601 (optional)")] string? to = null,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
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

        var request = new GetMessagesRequest
        {
            ChatId = TelegramClientToolInput.ToGuid(chatId),
            Page = TelegramClientToolInput.Page(page),
            PageSize = TelegramClientToolInput.PageSize(pageSize),
        };
        if (fromTimestamp is not null)
        {
            request.From = fromTimestamp;
        }

        if (toTimestamp is not null)
        {
            request.To = toTimestamp;
        }

        var response = await client.GetMessagesAsync(request, cancellationToken: cancellationToken);
        return TelegramClientToolOutput.Json(
            TelegramClientMapper.MapPage(response.Items, response.TotalCount, response.Page, response.PageSize, TelegramClientMapper.MapMessage));
    }
}
