using DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1;
using ChatMonitoringGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1.ChatMonitoringService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class AddMonitoredChatTool
{
    [McpServerTool(Name = "add_monitored_chat"), Description("Add a Telegram chat to monitoring.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ChatMonitoringGrpc.ChatMonitoringServiceClient client,
        [Description("Telegram session ID")] string sessionId,
        [Description("Public chat username, for example @channel")] string chatUsername,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!TelegramClientToolInput.Required(sessionId, nameof(sessionId), out var error) ||
            !TelegramClientToolInput.Required(chatUsername, nameof(chatUsername), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.AddAsync(
            new AddMonitoredChatRequest
            {
                SessionId = TelegramClientToolInput.ToGuid(sessionId),
                ChatUsername = TelegramClientToolInput.RequiredString(chatUsername),
            },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(TelegramClientMapper.MapChat(response));
    }
}
