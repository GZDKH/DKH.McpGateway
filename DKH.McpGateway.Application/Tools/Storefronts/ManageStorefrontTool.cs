using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Features.v1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ManageStorefrontTool
{
    [McpServerTool(Name = "manage_storefront"), Description(
        "Create, update, or delete a storefront. " +
        "For create: provide code, name, and optional description/features. " +
        "For update/delete: provide storefrontCode to identify the storefront.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        [Description("Action: create, update, or delete")] string action,
        [Description("Storefront code (required for update/delete, e.g. 'my-store')")] string? storefrontCode = null,
        [Description("Storefront name (for create/update)")] string? name = null,
        [Description("Storefront description (for create/update)")] string? description = null,
        [Description("Owner ID (required for create)")] string? ownerId = null,
        [Description("Enable cart feature (for create/update)")] bool? cartEnabled = null,
        [Description("Enable orders feature (for create/update)")] bool? ordersEnabled = null,
        [Description("Enable payments feature (for create/update)")] bool? paymentsEnabled = null,
        [Description("Enable reviews feature (for create/update)")] bool? reviewsEnabled = null,
        [Description("Enable wishlist feature (for create/update)")] bool? wishlistEnabled = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(storefrontCode) || string.IsNullOrEmpty(name))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "storefrontCode and name are required for create" },
                    McpJsonDefaults.Options);
            }

            var request = new CreateStorefrontRequest
            {
                Code = storefrontCode,
                Name = name,
                OwnerId = new GuidValue(ownerId ?? ""),
            };

            if (description is not null)
            {
                request.Description = description;
            }

            if (cartEnabled.HasValue || ordersEnabled.HasValue || paymentsEnabled.HasValue
                || reviewsEnabled.HasValue || wishlistEnabled.HasValue)
            {
                request.Features = new StorefrontFeaturesModel
                {
                    CartEnabled = cartEnabled ?? false,
                    OrdersEnabled = ordersEnabled ?? false,
                    PaymentsEnabled = paymentsEnabled ?? false,
                    ReviewsEnabled = reviewsEnabled ?? false,
                    WishlistEnabled = wishlistEnabled ?? false,
                };
            }

            var response = await client.CreateAsync(request, cancellationToken: cancellationToken);
            var s = response.Storefront;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "created",
                storefront = new { s.Id, s.Code, s.Name, status = s.Status.ToString() },
            }, McpJsonDefaults.Options);
        }

        if (string.IsNullOrEmpty(storefrontCode))
        {
            return JsonSerializer.Serialize(
                new { success = false, error = "storefrontCode is required for update/delete" },
                McpJsonDefaults.Options);
        }

        var storefront = await client.GetByCodeAsync(
            new GetStorefrontByCodeRequest { Code = storefrontCode },
            cancellationToken: cancellationToken);

        if (storefront.Storefront is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Storefront with code '{storefrontCode}' not found" },
                McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            var request = new UpdateStorefrontRequest
            {
                Id = storefront.Storefront.Id,
                Name = name ?? storefront.Storefront.Name,
                Features = storefront.Storefront.Features ?? new StorefrontFeaturesModel(),
            };

            if (description is not null)
            {
                request.Description = description;
            }

            if (cartEnabled.HasValue)
            {
                request.Features.CartEnabled = cartEnabled.Value;
            }

            if (ordersEnabled.HasValue)
            {
                request.Features.OrdersEnabled = ordersEnabled.Value;
            }

            if (paymentsEnabled.HasValue)
            {
                request.Features.PaymentsEnabled = paymentsEnabled.Value;
            }

            if (reviewsEnabled.HasValue)
            {
                request.Features.ReviewsEnabled = reviewsEnabled.Value;
            }

            if (wishlistEnabled.HasValue)
            {
                request.Features.WishlistEnabled = wishlistEnabled.Value;
            }

            var response = await client.UpdateAsync(request, cancellationToken: cancellationToken);
            var s = response.Storefront;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                storefront = new { s.Id, s.Code, s.Name, status = s.Status.ToString() },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            var response = await client.DeleteAsync(
                new DeleteStorefrontRequest { Id = storefront.Storefront.Id },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "deleted",
                deletedCode = storefrontCode,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }
}
