using DKH.TelegramClientService.Contracts.TelegramClient.Api.SessionManagement.V1;
using SessionManagementGrpc = DKH.TelegramClientService.Contracts.TelegramClient.Api.SessionManagement.V1.SessionManagementService;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

[McpServerToolType]
public static class ListTelegramSessionsTool
{
    [McpServerTool(Name = "list_telegram_sessions"), Description("List Telegram sessions with phone numbers omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SessionManagementGrpc.SessionManagementServiceClient client,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(
            new ListSessionsRequest
            {
                Page = TelegramClientToolInput.Page(page),
                PageSize = TelegramClientToolInput.PageSize(pageSize),
            },
            cancellationToken: cancellationToken);

        return TelegramClientToolOutput.Json(
            TelegramClientMapper.MapPage(response.Items, response.TotalCount, response.Page, response.PageSize, TelegramClientMapper.MapSession));
    }
}
