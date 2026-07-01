using DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1;
using ChatMonitoringGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1.ChatMonitoringService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class ListMonitoredChatsTool
{
    [McpServerTool(Name = "list_monitored_chats"), Description("List Telegram chats monitored by a session.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ChatMonitoringGrpc.ChatMonitoringServiceClient client,
        [Description("Telegram session ID")] string sessionId,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (!TelegramClientToolInput.Required(sessionId, nameof(sessionId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListAsync(
            new ListMonitoredChatsRequest
            {
                SessionId = TelegramClientToolInput.ToGuid(sessionId),
                Page = TelegramClientToolInput.Page(page),
                PageSize = TelegramClientToolInput.PageSize(pageSize),
            },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(
            TelegramClientMapper.MapPage(response.Items, response.TotalCount, response.Page, response.PageSize, TelegramClientMapper.MapChat));
    }
}
