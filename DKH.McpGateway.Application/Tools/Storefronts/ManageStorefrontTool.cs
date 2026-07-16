using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Features.v1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ManageStorefrontTool
{
    [McpServerTool(Name = "manage_storefront"), Description(
        "Update or delete a storefront. " +
        "Legacy create is disabled because it is not idempotent or workspace-safe; " +
        "stale create callers receive a migration error. " +
        "Provide storefrontCode to identify the storefront.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        [Description("Action: update or delete. Legacy create is rejected.")] string action,
        [Description("Storefront code (required, e.g. 'my-store')")] string? storefrontCode = null,
        [Description("Storefront name (for update)")] string? name = null,
        [Description("Storefront description (for update)")] string? description = null,
        [Description("Enable cart feature (for update)")] bool? cartEnabled = null,
        [Description("Enable orders feature (for update)")] bool? ordersEnabled = null,
        [Description("Enable payments feature (for update)")] bool? paymentsEnabled = null,
        [Description("Enable reviews feature (for update)")] bool? reviewsEnabled = null,
        [Description("Enable wishlist feature (for update)")] bool? wishlistEnabled = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                errorCode = "legacy_storefront_create_disabled",
                error = "Legacy storefront creation is disabled because it is not idempotent or workspace-safe.",
                nextAction = "Use provision_storefront_draft after the safe provisioning workflow is released.",
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
            new { success = false, error = $"Unknown action '{action}'. Use: update or delete" },
            McpJsonDefaults.Options);
    }
}
