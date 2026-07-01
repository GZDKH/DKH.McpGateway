using DKH.AssistantService.Contracts.Assistant.Models.V1;

namespace DKH.McpGateway.Application.Tools.Assistant;

internal static class AssistantChatRequestFactory
{
    internal static ChatRequest Create(
        string storefrontId,
        string sessionId,
        string message,
        string channel,
        string locale,
        string? userId,
        string? currency,
        string? catalogSeo)
    {
        var request = new ChatRequest
        {
            StorefrontId = AssistantToolInput.RequiredString(storefrontId),
            SessionId = AssistantToolInput.RequiredString(sessionId),
            Message = AssistantToolInput.RequiredString(message),
            Channel = AssistantToolInput.ParseChannel(channel),
            Locale = AssistantToolInput.RequiredString(locale),
        };

        AssistantToolInput.SetOptional(userId, value => request.UserId = value);
        AssistantToolInput.SetOptional(currency, value => request.Currency = value);
        AssistantToolInput.SetOptional(catalogSeo, value => request.CatalogSeo = value);

        return request;
    }
}
