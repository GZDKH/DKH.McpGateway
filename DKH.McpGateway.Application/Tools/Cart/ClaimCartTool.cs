using DKH.CartService.Contracts.Cart.Api.CartClaim.v1;

namespace DKH.McpGateway.Application.Tools.Cart;

[McpServerToolType]
public static class ClaimCartTool
{
    [McpServerTool(Name = "claim_cart"), Description(
        "POS-side operation: atomically transfer cart ownership to a cashier session using a claim code. " +
        "The cashier types or scans the code shown by the customer. " +
        "Returns NotFound if the code is unknown, FailedPrecondition if expired or already claimed. " +
        "Requires storeId and cashierUserId (GUIDs).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CartClaimService.CartClaimServiceClient client,
        [Description("Claim code typed or scanned by the cashier (e.g. FH4K)")] string code,
        [Description("Store ID (GUID) where the claim takes place")] string storeId,
        [Description("Cashier user ID (GUID)")] string cashierUserId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required");
        }

        if (string.IsNullOrWhiteSpace(storeId))
        {
            return McpProtoHelper.FormatError("storeId is required");
        }

        if (string.IsNullOrWhiteSpace(cashierUserId))
        {
            return McpProtoHelper.FormatError("cashierUserId is required");
        }

        var request = new ClaimCartRequest
        {
            Code = code,
            StoreId = new GuidValue(storeId),
            CashierUserId = new GuidValue(cashierUserId),
        };

        var response = await client.ClaimCartAsync(request, cancellationToken: cancellationToken);

        var cart = response.Cart;
        var claimCode = response.ClaimCode;

        return JsonSerializer.Serialize(new
        {
            claimCode = new
            {
                code = claimCode?.Code,
                cartId = claimCode?.CartId?.Value,
                status = claimCode?.Status.ToString(),
                claimedAt = claimCode?.ClaimedAt?.ToDateTimeOffset().ToString("O"),
                storeId = claimCode?.StoreId?.Value,
            },
            cart = cart is null
                ? null
                : new
                {
                    cartId = cart.CartId?.Value,
                    storefrontId = cart.StorefrontId?.Value,
                    currency = cart.Currency,
                    itemCount = cart.ItemCount,
                    subtotal = cart.Subtotal,
                    discountAmount = cart.DiscountAmount,
                    total = cart.Total,
                    items = cart.Items.Select(i => new
                    {
                        itemId = i.ItemId?.Value,
                        productId = i.ProductId?.Value,
                        variantId = i.VariantId?.Value,
                        title = i.Title,
                        quantity = i.Quantity,
                        price = i.Price,
                        currency = i.Currency,
                        purchaseType = i.PurchaseType.ToString(),
                    }),
                },
        }, McpJsonDefaults.Options);
    }
}
