using DKH.StorefrontService.Contracts.V1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class StorefrontOverviewTool
{
    [McpServerTool(Name = "storefront_overview"), Description("Get a comprehensive storefront overview: domains, channels, catalogs, and features in one call.")]
    public static async Task<string> ExecuteAsync(
        StorefrontCrudService.StorefrontCrudServiceClient crudClient,
        StorefrontDomainService.StorefrontDomainServiceClient domainClient,
        StorefrontChannelService.StorefrontChannelServiceClient channelClient,
        StorefrontCatalogService.StorefrontCatalogServiceClient catalogClient,
        [Description("Storefront ID (UUID)")] string storefrontId,
        CancellationToken cancellationToken = default)
    {
        var storefrontTask = crudClient.GetAsync(
            new GetStorefrontRequest { Id = storefrontId },
            cancellationToken: cancellationToken).ResponseAsync;

        var domainsTask = domainClient.GetDomainsAsync(
            new GetDomainsRequest { StorefrontId = storefrontId },
            cancellationToken: cancellationToken).ResponseAsync;

        var channelsTask = channelClient.GetChannelsAsync(
            new GetChannelsRequest { StorefrontId = storefrontId },
            cancellationToken: cancellationToken).ResponseAsync;

        var catalogsTask = catalogClient.GetCatalogsAsync(
            new GetCatalogsRequest { StorefrontId = storefrontId },
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(storefrontTask, domainsTask, channelsTask, catalogsTask);

        var s = storefrontTask.Result.Storefront;
        var result = new
        {
            id = s.Id,
            code = s.Code,
            name = s.Name,
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
            domains = domainsTask.Result.Domains.Select(static d => new
            {
                domain = d.Domain,
                isPrimary = d.IsPrimary,
                isVerified = d.IsVerified,
                sslStatus = d.SslStatus.ToString(),
            }),
            channels = channelsTask.Result.Channels.Select(static ch => new
            {
                channelType = ch.ChannelType.ToString(),
                displayName = ch.DisplayName,
                isActive = ch.IsActive,
                isDefault = ch.IsDefault,
                purpose = ch.Purpose.ToString(),
            }),
            catalogs = catalogsTask.Result.Catalogs.Select(static c => new
            {
                catalogId = c.CatalogId,
                displayName = c.DisplayName,
                displayOrder = c.DisplayOrder,
                isDefault = c.IsDefault,
                isVisible = c.IsVisible,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
