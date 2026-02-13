using DKH.StorefrontService.Contracts.V1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class GetStorefrontTool
{
    [McpServerTool(Name = "get_storefront"), Description("Get full storefront details including features by storefront ID or code.")]
    public static async Task<string> ExecuteAsync(
        StorefrontCrudService.StorefrontCrudServiceClient client,
        [Description("Storefront ID (UUID)")] string? storefrontId = null,
        [Description("Storefront code (alternative to ID)")] string? storefrontCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(storefrontId) && string.IsNullOrEmpty(storefrontCode))
        {
            return JsonSerializer.Serialize(new { error = "Provide either storefrontId or storefrontCode" }, McpJsonDefaults.Options);
        }

        GetStorefrontResponse storefront;

        if (!string.IsNullOrEmpty(storefrontCode))
        {
            var byCode = await client.GetByCodeAsync(
                new GetStorefrontByCodeRequest { Code = storefrontCode },
                cancellationToken: cancellationToken);
            storefront = new GetStorefrontResponse { Storefront = byCode.Storefront };
        }
        else
        {
            storefront = await client.GetAsync(
                new GetStorefrontRequest { Id = new GuidValue(storefrontId!) },
                cancellationToken: cancellationToken);
        }

        var s = storefront.Storefront;
        var result = new
        {
            id = s.Id,
            code = s.Code,
            name = s.Name,
            description = s.Description,
            status = s.Status.ToString(),
            features = s.Features is not null
                ? new
                {
                    cartEnabled = s.Features.CartEnabled,
                    ordersEnabled = s.Features.OrdersEnabled,
                    paymentsEnabled = s.Features.PaymentsEnabled,
                    reviewsEnabled = s.Features.ReviewsEnabled,
                    wishlistEnabled = s.Features.WishlistEnabled,
                }
                : null,
            createdAt = s.CreatedAt?.ToDateTimeOffset().ToString("O"),
            updatedAt = s.UpdatedAt?.ToDateTimeOffset().ToString("O"),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
