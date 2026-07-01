using DKH.AssistantService.Contracts.Assistant.Services.V1;

namespace DKH.McpGateway.Application.Tools.Assistant;

[McpServerToolType]
public static class AssistantChatTool
{
    [McpServerTool(Name = "assistant_chat"), Description(
        "Send a storefront assistant message and return the AI response, product cards, " +
        "suggestions, intent, cache flag, and session message count. Optional user ID is sent " +
        "only to AssistantService and is never echoed in the MCP response.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        AssistantChatService.AssistantChatServiceClient client,
        [Description("Storefront ID")] string storefrontId,
        [Description("Conversation session ID (stable per user conversation)")] string sessionId,
        [Description("User message text")] string message,
        [Description("Channel: telegram, web, or api (default api)")] string channel = "api",
        [Description("BCP-47 locale, e.g. en or ru")] string locale = "en",
        [Description("Optional customer identifier for personalization (not echoed)")] string? userId = null,
        [Description("Optional ISO-4217 currency for prices")] string? currency = null,
        [Description("Optional catalog SEO scope")] string? catalogSeo = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!Validate(storefrontId, sessionId, message, out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ChatAsync(
            AssistantChatRequestFactory.Create(storefrontId, sessionId, message, channel, locale, userId, currency, catalogSeo),
            cancellationToken: cancellationToken);

        return AssistantToolOutput.Json(AssistantMapper.MapResponse(response));
    }

    internal static bool Validate(string storefrontId, string sessionId, string message, out string error)
    {
        if (!AssistantToolInput.Required(storefrontId, nameof(storefrontId), out var validationError) ||
            !AssistantToolInput.Required(sessionId, nameof(sessionId), out validationError) ||
            !AssistantToolInput.Required(message, nameof(message), out validationError))
        {
            error = validationError;
            return false;
        }

        error = "";
        return true;
    }
}
