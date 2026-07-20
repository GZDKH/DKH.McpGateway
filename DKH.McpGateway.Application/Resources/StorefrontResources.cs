using DKH.McpGateway.Application.Tools.Storefronts;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontBrandingManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Resources;

[McpServerResourceType]
[Authorize(Policy = MerchantResourceAuthorization.PolicyName)]
public static class StorefrontResources
{
    [McpServerResource(Name = "storefront://storefronts", MimeType = "application/json")]
    [Description("All storefronts with their status and basic configuration.")]
    public static async Task<string> GetStorefrontsAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        CancellationToken cancellationToken = default)
    {
        var scope = StorefrontWorkspaceScope.Resolve(apiKeyContext, httpContextAccessor);
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await scope.GetAllAsync(
            client,
            page: 1,
            pageSize: 50,
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            totalCount = response.Pagination.TotalCount,
            storefronts = response.Storefronts.Select(static s => new
            {
                id = s.Id,
                code = s.Code,
                name = s.Name,
                status = s.Status.ToString(),
                createdAt = s.CreatedAt?.ToDateTimeOffset().ToString("O"),
            }),
        }, McpJsonDefaults.Options);
    }

    [McpServerResource(Name = "storefront://config", MimeType = "application/json")]
    [Description("Full configuration for a specific storefront by code.")]
    public static async Task<string> GetStorefrontConfigAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        StorefrontsCrudService.StorefrontsCrudServiceClient crudClient,
        StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient brandingClient,
        StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient featuresClient,
        [Description("Storefront code, e.g. 'main'")] string storefrontCode,
        CancellationToken cancellationToken = default)
    {
        var scope = StorefrontWorkspaceScope.Resolve(apiKeyContext, httpContextAccessor);
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var storefront = await scope.GetByCodeAsync(crudClient, storefrontCode, cancellationToken);

        var brandingTask = brandingClient.GetBrandingAsync(
            new GetBrandingRequest { StorefrontId = storefront.Id },
            scope.Headers,
            cancellationToken: cancellationToken).ResponseAsync;

        var featuresTask = featuresClient.GetFeaturesAsync(
            new GetFeaturesRequest { StorefrontId = storefront.Id },
            scope.Headers,
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(brandingTask, featuresTask);

        var b = brandingTask.Result.Branding;
        var f = featuresTask.Result.Features;

        return JsonSerializer.Serialize(new
        {
            code = storefront.Code,
            name = storefront.Name,
            status = storefront.Status.ToString(),
            branding = b is not null
                ? new
                {
                    logoAttachmentId = b.LogoAttachmentId?.Value,
                    faviconAttachmentId = b.FaviconAttachmentId?.Value,
                    primaryColor = b.Colors?.Primary,
                    secondaryColor = b.Colors?.Secondary,
                }
                : null,
            features = f is not null
                ? new
                {
                    f.CartEnabled,
                    f.OrdersEnabled,
                    f.PaymentsEnabled,
                    f.ReviewsEnabled,
                    f.WishlistEnabled,
                }
                : null,
        }, McpJsonDefaults.Options);
    }
}
