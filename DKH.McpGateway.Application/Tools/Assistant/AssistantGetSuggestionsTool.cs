using DKH.AssistantService.Contracts.Assistant.Models.V1;
using DKH.AssistantService.Contracts.Assistant.Services.V1;

namespace DKH.McpGateway.Application.Tools.Assistant;

[McpServerToolType]
public static class AssistantGetSuggestionsTool
{
    [McpServerTool(Name = "assistant_get_suggestions"), Description(
        "Get conversation-aware assistant suggestions for a storefront session.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AssistantChatService.AssistantChatServiceClient client,
        [Description("Storefront ID")] string storefrontId,
        [Description("Conversation session ID (stable per user conversation)")] string sessionId,
        [Description("Channel: telegram, web, or api (default api)")] string channel = "api",
        [Description("BCP-47 locale, e.g. en or ru")] string locale = "en",
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (!AssistantToolInput.Required(storefrontId, nameof(storefrontId), out var error) ||
            !AssistantToolInput.Required(sessionId, nameof(sessionId), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetSuggestionsAsync(
            new GetSuggestionsRequest
            {
                StorefrontId = AssistantToolInput.RequiredString(storefrontId),
                SessionId = AssistantToolInput.RequiredString(sessionId),
                Channel = AssistantToolInput.ParseChannel(channel),
                Locale = AssistantToolInput.RequiredString(locale),
            },
            cancellationToken: cancellationToken);

        return AssistantToolOutput.Json(new { suggestions = response.Suggestions.Select(AssistantMapper.MapSuggestion) });
    }
}
