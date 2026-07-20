using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class GetStorefrontFeaturesTool
{
    [McpServerTool(Name = "get_storefront_features"), Description("Get storefront feature flags: cart, orders, payments, reviews, wishlist.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        StorefrontsCrudService.StorefrontsCrudServiceClient crudClient,
        StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient client,
        [Description("Storefront ID (UUID)")] string storefrontId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        var scope = StorefrontWorkspaceScope.Resolve(apiKeyContext, httpContextAccessor);
        var storefront = await scope.GetByIdAsync(crudClient, storefrontId, cancellationToken);
        var response = await client.GetFeaturesAsync(
            new GetFeaturesRequest { StorefrontId = storefront.Id },
            scope.Headers,
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
