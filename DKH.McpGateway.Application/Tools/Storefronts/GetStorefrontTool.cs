using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Storefront.v1;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class GetStorefrontTool
{
    [McpServerTool(Name = "get_storefront"), Description("Get full storefront details including features by storefront ID or code.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        [Description("Storefront ID (UUID)")] string? storefrontId = null,
        [Description("Storefront code (alternative to ID)")] string? storefrontCode = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrEmpty(storefrontId) && string.IsNullOrEmpty(storefrontCode))
        {
            return JsonSerializer.Serialize(new { error = "Provide either storefrontId or storefrontCode" }, McpJsonDefaults.Options);
        }

        var scope = StorefrontWorkspaceScope.Resolve(apiKeyContext, httpContextAccessor);
        StorefrontModel storefront;

        if (!string.IsNullOrEmpty(storefrontCode))
        {
            storefront = await scope.GetByCodeAsync(client, storefrontCode, cancellationToken);
        }
        else
        {
            storefront = await scope.GetByIdAsync(client, storefrontId!, cancellationToken);
        }

        var s = storefront;
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
