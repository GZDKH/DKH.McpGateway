using DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1;
using MessageCollectionGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1.MessageCollectionService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class SearchMessagesTool
{
    [McpServerTool(Name = "search_messages"), Description("Search archived Telegram messages in a monitored chat.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        MessageCollectionGrpc.MessageCollectionServiceClient client,
        [Description("Monitored chat ID")] string chatId,
        [Description("Search text")] string searchText,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (!TelegramClientToolInput.Required(chatId, nameof(chatId), out var error) ||
            !TelegramClientToolInput.Required(searchText, nameof(searchText), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.SearchMessagesAsync(
            new SearchMessagesRequest
            {
                ChatId = TelegramClientToolInput.ToGuid(chatId),
                SearchText = TelegramClientToolInput.RequiredString(searchText),
                Page = TelegramClientToolInput.Page(page),
                PageSize = TelegramClientToolInput.PageSize(pageSize),
            },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(
            TelegramClientMapper.MapPage(response.Items, response.TotalCount, response.Page, response.PageSize, TelegramClientMapper.MapMessage));
    }
}
