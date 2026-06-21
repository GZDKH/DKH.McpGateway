using DKH.CartService.Contracts.Cart.Api.CartCrud.v1;

namespace DKH.McpGateway.Application.Tools.Cart;

[McpServerToolType]
public static class ListCartsTool
{
    [McpServerTool(Name = "list_carts"), Description(
        "List shopping carts with optional filtering. " +
        "Provide storefrontId to scope to a storefront; omit to list across all storefronts. " +
        "Filter by hasItems (true/false), softDeleteFilter (active/deleted/all). " +
        "Supports pagination via page and pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CartCrudService.CartCrudServiceClient client,
        [Description("Storefront ID (GUID, optional — omit for all storefronts)")] string? storefrontId = null,
        [Description("Filter: only carts with items (true), empty carts (false), or all (omit)")] bool? hasItems = null,
        [Description("Soft-delete filter: active (default), deleted, all")] string? softDeleteFilter = null,
        [Description("Start of date range (ISO 8601)")] string? from = null,
        [Description("End of date range (ISO 8601)")] string? to = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListCartsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(storefrontId))
        {
            request.StorefrontId = new GuidValue(storefrontId);
        }

        if (hasItems.HasValue)
        {
            request.HasItems = hasItems.Value;
        }

        if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
        {
            request.From = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(fromDate.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
        {
            request.To = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(toDate.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(softDeleteFilter))
        {
            request.SoftDeleteFilter = softDeleteFilter.ToLowerInvariant() switch
            {
                "deleted" => SoftDeleteFilter.InactiveOnly,
                "all" => SoftDeleteFilter.All,
                _ => SoftDeleteFilter.ActiveOnly,
            };
        }

        var response = await client.ListCartsAsync(request, cancellationToken: cancellationToken);

        var safeItems = response.Items.Select(cart => new
        {
            cartId = cart.CartId?.Value,
            storefrontId = cart.StorefrontId?.Value,
            currency = cart.Currency,
            itemCount = cart.ItemCount,
            subtotal = cart.Subtotal,
            discountAmount = cart.DiscountAmount,
            total = cart.Total,
            hasPromoCode = cart.AppliedPromoCode is not null,
            requiresShippingRequote = cart.RequiresShippingRequote,
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
                purchaseType = i.PurchaseType.ToString(),
            }),
        });

        return JsonSerializer.Serialize(new
        {
            items = safeItems,
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
