using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontBrandingManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using Microsoft.Extensions.Caching.Memory;

namespace DKH.McpGateway.Application.Resources;

[McpServerResourceType]
public static class StorefrontResources
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    [McpServerResource(Name = "storefront://storefronts", MimeType = "application/json")]
    [Description("All storefronts with their status and basic configuration.")]
    public static async Task<string> GetStorefrontsAsync(
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        IMemoryCache cache,
        CancellationToken cancellationToken = default)
    {
        return (await cache.GetOrCreateAsync("storefront://storefronts", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var response = await client.GetAllAsync(
                new GetAllStorefrontsRequest { Pagination = new PaginationRequest { Page = 1, PageSize = 50 } },
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
        }))!;
    }

    [McpServerResource(Name = "storefront://config", MimeType = "application/json")]
    [Description("Full configuration for a specific storefront by code.")]
    public static async Task<string> GetStorefrontConfigAsync(
        StorefrontsCrudService.StorefrontsCrudServiceClient crudClient,
        StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient brandingClient,
        StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient featuresClient,
        IMemoryCache cache,
        [Description("Storefront code, e.g. 'main'")] string storefrontCode,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"storefront://config:{storefrontCode}";
        return (await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var storefrontResponse = await crudClient.GetByCodeAsync(
                new GetStorefrontByCodeRequest { Code = storefrontCode },
                cancellationToken: cancellationToken);

            var storefront = storefrontResponse.Storefront;

            var brandingTask = brandingClient.GetBrandingAsync(
                new GetBrandingRequest { StorefrontId = storefront.Id },
                cancellationToken: cancellationToken).ResponseAsync;

            var featuresTask = featuresClient.GetFeaturesAsync(
                new GetFeaturesRequest { StorefrontId = storefront.Id },
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
                        logo = b.Logo,
                        favicon = b.Favicon,
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
        }))!;
    }
}
