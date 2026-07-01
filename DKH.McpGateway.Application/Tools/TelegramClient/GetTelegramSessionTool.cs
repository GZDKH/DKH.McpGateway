using DKH.TelegramClientService.Contracts.TelegramClient.Api.SessionManagement.V1;
using SessionManagementGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.SessionManagement.V1.SessionManagementService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class GetTelegramSessionTool
{
    [McpServerTool(Name = "get_telegram_session"), Description("Get a Telegram session with phone number omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SessionManagementGrpc.SessionManagementServiceClient client,
        [Description("Telegram session ID")] string sessionId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (!TelegramClientToolInput.Required(sessionId, nameof(sessionId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetAsync(
            new GetSessionRequest { SessionId = TelegramClientToolInput.ToGuid(sessionId) },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(TelegramClientMapper.MapSession(response));
    }
}
