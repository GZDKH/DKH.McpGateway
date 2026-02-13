using DKH.StorefrontService.Contracts.V1;

namespace DKH.McpGateway.Application.Resources;

[McpServerResourceType]
public static class StorefrontResources
{
    [McpServerResource(Name = "storefront://storefronts", MimeType = "application/json")]
    [Description("All storefronts with their status and basic configuration.")]
    public static async Task<string> GetStorefrontsAsync(
        StorefrontCrudService.StorefrontCrudServiceClient client,
        CancellationToken cancellationToken = default)
    {
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
    }

    [McpServerResource(Name = "storefront://config", MimeType = "application/json")]
    [Description("Full configuration for a specific storefront by code.")]
    public static async Task<string> GetStorefrontConfigAsync(
        StorefrontCrudService.StorefrontCrudServiceClient crudClient,
        StorefrontBrandingService.StorefrontBrandingServiceClient brandingClient,
        StorefrontFeaturesService.StorefrontFeaturesServiceClient featuresClient,
        [Description("Storefront code, e.g. 'main'")] string storefrontCode,
        CancellationToken cancellationToken = default)
    {
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
    }
}
