using System.Text;
using DKH.AssistantService.Contracts.Assistant.Models.V1;
using DKH.AssistantService.Contracts.Assistant.Services.V1;
using Grpc.Core;

namespace DKH.McpGateway.Application.Tools.Assistant;

[McpServerToolType]
public static class AssistantChatStreamTool
{
    [McpServerTool(Name = "assistant_chat_stream"), Description(
        "Send a storefront assistant message through the streaming chat RPC and return one " +
        "aggregated JSON response with combined text deltas, product cards, suggestions, " +
        "intent, session message count, and products found.")]
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
        if (!AssistantChatTool.Validate(storefrontId, sessionId, message, out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        using var call = client.ChatStream(
            AssistantChatRequestFactory.Create(storefrontId, sessionId, message, channel, locale, userId, currency, catalogSeo),
            cancellationToken: cancellationToken);

        var text = new StringBuilder();
        var products = new List<object>();
        var suggestions = new List<object>();
        string? intent = null;
        int? sessionMessageCount = null;
        int? productsFound = null;

        await foreach (var chunk in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            switch (chunk.ChunkCase)
            {
                case ChatStreamChunk.ChunkOneofCase.TextDelta:
                    text.Append(chunk.TextDelta);
                    break;
                case ChatStreamChunk.ChunkOneofCase.Product:
                    products.Add(AssistantMapper.MapProductCard(chunk.Product));
                    break;
                case ChatStreamChunk.ChunkOneofCase.Suggestion:
                    suggestions.Add(AssistantMapper.MapSuggestion(chunk.Suggestion));
                    break;
                case ChatStreamChunk.ChunkOneofCase.Meta:
                    intent = chunk.Meta.Intent.ToString();
                    sessionMessageCount = chunk.Meta.SessionMessageCount;
                    productsFound = chunk.Meta.ProductsFound;
                    break;
            }
        }

        return AssistantToolOutput.Json(new
        {
            text = text.ToString(),
            products,
            suggestions,
            intent,
            sessionMessageCount,
            productsFound,
        });
    }
}
