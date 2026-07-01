using DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1;
using ChatMonitoringGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1.ChatMonitoringService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class TriggerBackfillTool
{
    [McpServerTool(Name = "trigger_backfill"), Description("Trigger Telegram chat history backfill.")]
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

        await client.TriggerBackfillAsync(
            new TriggerBackfillRequest { ChatId = TelegramClientToolInput.ToGuid(chatId) },
            cancellationToken: cancellationToken);

        return McpProtoHelper.FormatOk();
    }
}
