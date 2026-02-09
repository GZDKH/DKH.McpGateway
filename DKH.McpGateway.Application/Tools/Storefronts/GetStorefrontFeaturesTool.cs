using DKH.StorefrontService.Contracts.V1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class GetStorefrontFeaturesTool
{
    [McpServerTool(Name = "get_storefront_features"), Description("Get storefront feature flags: cart, orders, payments, reviews, wishlist.")]
    public static async Task<string> ExecuteAsync(
        StorefrontFeaturesService.StorefrontFeaturesServiceClient client,
        [Description("Storefront ID (UUID)")] string storefrontId,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetFeaturesAsync(
            new GetFeaturesRequest { StorefrontId = storefrontId },
            cancellationToken: cancellationToken);

        var f = response.Features;
        var result = new
        {
            storefrontId,
            features = new
            {
                cartEnabled = f.CartEnabled,
                ordersEnabled = f.OrdersEnabled,
                paymentsEnabled = f.PaymentsEnabled,
                reviewsEnabled = f.ReviewsEnabled,
                wishlistEnabled = f.WishlistEnabled,
            },
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
