using DKH.AssistantService.Contracts.Assistant.Models.V1;

namespace DKH.McpGateway.Application.Tools.Assistant;

internal static class AssistantMapper
{
    internal static object MapResponse(ChatResponse response) => new
    {
        text = response.Text,
        products = response.Products.Select(MapProductCard),
        suggestions = response.Suggestions.Select(MapSuggestion),
        intent = response.Intent.ToString(),
        fromCache = response.FromCache,
        sessionMessageCount = response.SessionMessageCount,
    };

    internal static object MapProductCard(ProductCardModel product) => new
    {
        id = product.Id,
        code = product.Code,
        name = product.Name,
        description = NullIfEmpty(product.Description),
        price = product.Price,
        oldPrice = product.HasOldPrice ? product.OldPrice : (double?)null,
        currency = product.Currency,
        imageUrl = NullIfEmpty(product.ImageUrl),
        productUrl = NullIfEmpty(product.ProductUrl),
        rating = product.Rating,
        reviewCount = product.ReviewCount,
        inStock = product.InStock,
        brand = NullIfEmpty(product.Brand),
        keySpecs = product.KeySpecs.Select(spec => new { name = spec.Name, value = spec.Value }),
        matchReason = product.HasMatchReason ? product.MatchReason : null,
    };

    internal static object MapSuggestion(SuggestionModel suggestion) => new
    {
        text = suggestion.Text,
        type = suggestion.Type.ToString(),
    };

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
