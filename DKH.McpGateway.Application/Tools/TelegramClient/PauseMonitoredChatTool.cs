using DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1;
using ChatMonitoringGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1.ChatMonitoringService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class PauseMonitoredChatTool
{
    [McpServerTool(Name = "pause_monitored_chat"), Description("Pause Telegram chat monitoring.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ChatMonitoringGrpc.ChatMonitoringServiceClient client,
        [Description("Monitored chat ID")] string chatId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!TelegramClientToolInput.Required(chatId, nameof(chatId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.PauseAsync(
            new PauseMonitoredChatRequest { ChatId = TelegramClientToolInput.ToGuid(chatId) },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(TelegramClientMapper.MapChat(response));
    }
}
