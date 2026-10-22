using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;

namespace DKH.McpGateway.Application.Tools.Common;

/// <summary>Display projection shared by actual search and recommendation tool paths.</summary>
internal static class ProductSearchProjection
{
    internal static Dictionary<string, object?> Create(ProductSearchModel product, double? distance = null)
    {
        var quote = product.HasCallForPrice && product.CallForPrice;
        var result = new Dictionary<string, object?>
        {
            ["id"] = product.Id,
            ["code"] = product.Code,
            ["name"] = product.Name,
            ["seoName"] = string.IsNullOrEmpty(product.SeoName) ? product.Code : product.SeoName,
            // Preserve the established quote display policy; absence alone is not a quote.
            ["price"] = !quote && product.HasPrice ? (double?)product.Price : null,
            ["currency"] = product.HasCurrency ? product.Currency : null,
            ["brand"] = string.IsNullOrEmpty(product.Brand) ? null : product.Brand,
            ["inStock"] = product.HasInStock ? (bool?)product.InStock : null,
        };
        if (distance.HasValue)
        {
            result["distance"] = distance.Value;
        }

        return result;
    }
}
