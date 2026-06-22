using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;

namespace DKH.McpGateway.Application.Tools.StorefrontPublic;

/// <summary>
/// Shared per-tenant guard + catalog resolution for the public <c>storefront_*</c> tool namespace.
/// Enforces <see cref="ApiKeyScope.Storefront"/>, the <c>McpEnabled</c> gate, and resolves the
/// storefront's catalog SEO names — the basis for isolating every downstream catalog read to a
/// single storefront (ADR-060). A storefront-scoped key can never reach another storefront's data.
/// </summary>
internal static class StorefrontScope
{
    /// <summary>
    /// Ensures the request carries a storefront-scoped key and returns its bound storefront id.
    /// </summary>
    public static Guid RequireStorefrontId(IApiKeyContext apiKeyContext)
    {
        if (apiKeyContext.Scope != ApiKeyScope.Storefront)
        {
            throw new UnauthorizedAccessException(
                "This tool requires a storefront-scoped API key.");
        }

        return apiKeyContext.StorefrontId
            ?? throw new UnauthorizedAccessException(
                "This tool requires a storefront-scoped API key.");
    }

    /// <summary>
    /// Validates the caller is a storefront-scoped key with MCP access enabled, then returns the
    /// SEO names of the catalogs linked to that storefront (empty when none are linked/visible).
    /// </summary>
    public static async Task<IReadOnlyList<string>> ResolveCatalogSeoNamesAsync(
        IApiKeyContext apiKeyContext,
        IStorefrontMcpGate gate,
        StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient storefrontCatalogClient,
        CatalogManagementService.CatalogManagementServiceClient catalogClient,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var storefrontId = RequireStorefrontId(apiKeyContext);
        await gate.EnsureMcpEnabledAsync(storefrontId, cancellationToken);

        var linked = await storefrontCatalogClient.GetCatalogsAsync(
            new GetCatalogsRequest { StorefrontId = new GuidValue(storefrontId.ToString()) },
            cancellationToken: cancellationToken);

        var catalogIds = linked.Catalogs
            .Where(static c => c.IsVisible)
            .Select(static c => c.CatalogId?.Value)
            .Where(static id => !string.IsNullOrEmpty(id))
            .ToList();

        if (catalogIds.Count == 0)
        {
            return [];
        }

        var detailRequest = new GetStorefrontCatalogsRequest { LanguageCode = languageCode };
        detailRequest.CatalogIds.AddRange(catalogIds!);

        var detail = await catalogClient.GetCatalogsAsync(
            detailRequest,
            cancellationToken: cancellationToken);

        return [.. detail.Catalogs
            .Select(static c => c.SeoName)
            .Where(static s => !string.IsNullOrEmpty(s))];
    }
}
