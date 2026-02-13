using DKH.StorefrontService.Contracts.V1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ManageStorefrontCatalogsTool
{
    [McpServerTool(Name = "manage_storefront_catalogs"), Description(
        "Manage catalogs linked to a storefront. " +
        "Actions: 'list' to view linked catalogs, 'add' to link a catalog, " +
        "'remove' to unlink, 'set_default' to set the default catalog.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StorefrontCrudService.StorefrontCrudServiceClient crudClient,
        StorefrontCatalogService.StorefrontCatalogServiceClient catalogClient,
        [Description("Storefront code (e.g. 'my-store')")] string storefrontCode,
        [Description("Action: list, add, remove, or set_default")] string action,
        [Description("Catalog ID to add/remove/set_default")] string? catalogId = null,
        [Description("Catalog link ID (for remove/set_default, returned from list)")] string? catalogLinkId = null,
        [Description("Display order (for add)")] int? displayOrder = null,
        [Description("Is default catalog (for add)")] bool? isDefault = null,
        [Description("Is visible (for add)")] bool? isVisible = null,
        [Description("Custom display name (for add)")] string? displayName = null,
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

        if (string.Equals(action, "list", StringComparison.OrdinalIgnoreCase))
        {
            var response = await catalogClient.GetCatalogsAsync(
                new GetCatalogsRequest { StorefrontId = storefrontId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                catalogs = response.Catalogs.Select(c => new
                {
                    linkId = c.Id,
                    catalogId = c.CatalogId,
                    displayOrder = c.DisplayOrder,
                    isDefault = c.IsDefault,
                    isVisible = c.IsVisible,
                    displayName = c.DisplayName,
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "add", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(catalogId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "catalogId is required for add" },
                    McpJsonDefaults.Options);
            }

            var request = new AddCatalogRequest
            {
                StorefrontId = storefrontId,
                CatalogId = new GuidValue(catalogId),
                DisplayOrder = displayOrder ?? 0,
                IsDefault = isDefault ?? false,
                IsVisible = isVisible ?? true,
            };

            if (displayName is not null)
            {
                request.DisplayName = displayName;
            }

            var response = await catalogClient.AddCatalogAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "added",
                catalog = new { linkId = response.Catalog.Id, response.Catalog.CatalogId },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(catalogLinkId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "catalogLinkId is required for remove (get it from 'list' action)" },
                    McpJsonDefaults.Options);
            }

            var response = await catalogClient.RemoveCatalogAsync(
                new RemoveCatalogRequest { StorefrontId = storefrontId, CatalogLinkId = new GuidValue(catalogLinkId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "removed",
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "set_default", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(catalogLinkId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "catalogLinkId is required for set_default" },
                    McpJsonDefaults.Options);
            }

            var response = await catalogClient.SetDefaultCatalogAsync(
                new SetDefaultCatalogRequest { StorefrontId = storefrontId, CatalogLinkId = new GuidValue(catalogLinkId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "set_default",
                catalog = new { linkId = response.Catalog.Id, response.Catalog.CatalogId, response.Catalog.IsDefault },
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: list, add, remove, or set_default" },
            McpJsonDefaults.Options);
    }
}
