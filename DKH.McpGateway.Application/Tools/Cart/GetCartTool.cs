using DKH.CartService.Contracts.Cart.Api.CartCrud.v1;

namespace DKH.McpGateway.Application.Tools.Cart;

[McpServerToolType]
public static class GetCartTool
{
    [McpServerTool(Name = "get_cart"), Description(
        "Get a single cart by its ID. " +
        "Optionally scope to a storefront via storefrontId. " +
        "Returns cart details with items and totals. Customer PII (phone, address) is omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CartCrudService.CartCrudServiceClient client,
        [Description("Cart ID (GUID)")] string cartId,
        [Description("Storefront ID (GUID, optional)")] string? storefrontId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(cartId))
        {
            return McpProtoHelper.FormatError("cartId is required");
        }

        var request = new GetCartRequest { CartId = cartId };

        if (!string.IsNullOrWhiteSpace(storefrontId))
        {
            request.StorefrontId = new GuidValue(storefrontId);
        }

        var cart = await client.GetCartAsync(request, cancellationToken: cancellationToken);

        var safe = new
        {
            cartId = cart.CartId?.Value,
            storefrontId = cart.StorefrontId?.Value,
            currency = cart.Currency,
            itemCount = cart.ItemCount,
            subtotal = cart.Subtotal,
            discountAmount = cart.DiscountAmount,
            total = cart.Total,
            comment = cart.Comment,
            ttlSeconds = cart.TtlSeconds,
            snapshotId = cart.SnapshotId?.Value,
            hasPromoCode = cart.AppliedPromoCode is not null,
            promoCode = cart.AppliedPromoCode?.Code,
            requiresShippingRequote = cart.RequiresShippingRequote,
            selectedShipping = cart.SelectedShippingOption is null
                ? null
                : new
                {
                    carrier = cart.SelectedShippingOption.Carrier,
                    serviceLevel = cart.SelectedShippingOption.ServiceLevel,
                    price = cart.SelectedShippingOption.Price,
                    currency = cart.SelectedShippingOption.Currency,
                },
            createdAt = cart.CreatedAt?.ToDateTimeOffset().ToString("O"),
            updatedAt = cart.UpdatedAt?.ToDateTimeOffset().ToString("O"),
            items = cart.Items.Select(i => new
            {
                itemId = i.ItemId?.Value,
                productId = i.ProductId?.Value,
                variantId = i.VariantId?.Value,
                title = i.Title,
                quantity = i.Quantity,
                price = i.Price,
                currency = i.Currency,
                note = i.Note,
                purchaseType = i.PurchaseType.ToString(),
                packageId = i.PackageId?.Value,
                packageQuantity = i.HasPackageQuantity ? i.PackageQuantity : (int?)null,
                originalUnitPrice = i.HasOriginalUnitPrice ? i.OriginalUnitPrice : (double?)null,
                tierQuantityThreshold = i.HasTierQuantityThreshold ? i.TierQuantityThreshold : (int?)null,
            }),
        };

        return JsonSerializer.Serialize(safe, McpJsonDefaults.Options);
    }
}
