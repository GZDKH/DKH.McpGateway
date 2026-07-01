using DKH.AssistantService.Contracts.Assistant.Models.V1;
using DKH.AssistantService.Contracts.Assistant.Services.V1;

namespace DKH.McpGateway.Application.Tools.Assistant;

[McpServerToolType]
public static class AssistantClearSessionTool
{
    [McpServerTool(Name = "assistant_clear_session"), Description(
        "Clear the AssistantService conversation context for a storefront session.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AssistantChatService.AssistantChatServiceClient client,
        [Description("Storefront ID")] string storefrontId,
        [Description("Conversation session ID (stable per user conversation)")] string sessionId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!AssistantToolInput.Required(storefrontId, nameof(storefrontId), out var error) ||
            !AssistantToolInput.Required(sessionId, nameof(sessionId), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        await client.ClearSessionAsync(
            new ClearSessionRequest
            {
                StorefrontId = AssistantToolInput.RequiredString(storefrontId),
                SessionId = AssistantToolInput.RequiredString(sessionId),
            },
            cancellationToken: cancellationToken);

        return AssistantToolOutput.Json(new
        {
            cleared = true,
            storefrontId = AssistantToolInput.RequiredString(storefrontId),
            sessionId = AssistantToolInput.RequiredString(sessionId),
        });
    }
}
