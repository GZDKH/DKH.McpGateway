using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Features.v1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ManageStorefrontFeaturesTool
{
    [McpServerTool(Name = "manage_storefront_features"), Description(
        "Toggle storefront features: cart, orders, payments, reviews, wishlist. " +
        "Actions: 'get' to view features, 'enable' or 'disable' a specific feature, " +
        "'update' to set multiple features at once.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StorefrontsCrudService.StorefrontsCrudServiceClient crudClient,
        StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient featuresClient,
        [Description("Storefront code (e.g. 'my-store')")] string storefrontCode,
        [Description("Action: get, enable, disable, or update")] string action,
        [Description("Feature name for enable/disable: cart, orders, payments, reviews, wishlist")] string? featureName = null,
        [Description("Enable cart (for update action)")] bool? cartEnabled = null,
        [Description("Enable orders (for update action)")] bool? ordersEnabled = null,
        [Description("Enable payments (for update action)")] bool? paymentsEnabled = null,
        [Description("Enable reviews (for update action)")] bool? reviewsEnabled = null,
        [Description("Enable wishlist (for update action)")] bool? wishlistEnabled = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var storefront = await crudClient.GetByCodeAsync(
            new GetStorefrontByCodeRequest { Code = storefrontCode },
            cancellationToken: cancellationToken);

        if (storefront.Storefront is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Storefront '{storefrontCode}' not found" },
                McpJsonDefaults.Options);
        }

        var storefrontId = storefront.Storefront.Id;

        if (string.Equals(action, "get", StringComparison.OrdinalIgnoreCase))
        {
            var response = await featuresClient.GetFeaturesAsync(
                new GetFeaturesRequest { StorefrontId = storefrontId },
                cancellationToken: cancellationToken);

            var f = response.Features;
            return JsonSerializer.Serialize(new
            {
                success = true,
                features = new
                {
                    cartEnabled = f.CartEnabled,
                    ordersEnabled = f.OrdersEnabled,
                    paymentsEnabled = f.PaymentsEnabled,
                    reviewsEnabled = f.ReviewsEnabled,
                    wishlistEnabled = f.WishlistEnabled,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "enable", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(featureName))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "featureName is required for enable (cart, orders, payments, reviews, wishlist)" },
                    McpJsonDefaults.Options);
            }

            var response = await featuresClient.EnableFeatureAsync(
                new EnableFeatureRequest { StorefrontId = storefrontId, FeatureName = featureName },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "enabled",
                feature = featureName,
                features = FormatFeatures(response.Features),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "disable", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(featureName))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "featureName is required for disable" },
                    McpJsonDefaults.Options);
            }

            var response = await featuresClient.DisableFeatureAsync(
                new DisableFeatureRequest { StorefrontId = storefrontId, FeatureName = featureName },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "disabled",
                feature = featureName,
                features = FormatFeatures(response.Features),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            var current = await featuresClient.GetFeaturesAsync(
                new GetFeaturesRequest { StorefrontId = storefrontId },
                cancellationToken: cancellationToken);

            var features = current.Features ?? new StorefrontFeaturesModel();
            if (cartEnabled.HasValue)
            {
                features.CartEnabled = cartEnabled.Value;
            }

            if (ordersEnabled.HasValue)
            {
                features.OrdersEnabled = ordersEnabled.Value;
            }

            if (paymentsEnabled.HasValue)
            {
                features.PaymentsEnabled = paymentsEnabled.Value;
            }

            if (reviewsEnabled.HasValue)
            {
                features.ReviewsEnabled = reviewsEnabled.Value;
            }

            if (wishlistEnabled.HasValue)
            {
                features.WishlistEnabled = wishlistEnabled.Value;
            }

            var response = await featuresClient.UpdateFeaturesAsync(
                new UpdateFeaturesRequest { StorefrontId = storefrontId, Features = features },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                features = FormatFeatures(response.Features),
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: get, enable, disable, or update" },
            McpJsonDefaults.Options);
    }

    private static object FormatFeatures(StorefrontFeaturesModel f) => new
    {
        cartEnabled = f.CartEnabled,
        ordersEnabled = f.OrdersEnabled,
        paymentsEnabled = f.PaymentsEnabled,
        reviewsEnabled = f.ReviewsEnabled,
        wishlistEnabled = f.WishlistEnabled,
    };
}
